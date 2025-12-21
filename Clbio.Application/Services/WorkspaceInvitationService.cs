using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.WorkspaceInvitation;
using Clbio.Application.DTOs.V1.WorkspaceMember;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class WorkspaceInvitationService(
        IUnitOfWork uow,
        INotificationAppService notificationService,
        IMapper mapper,
        ICacheService cache,
        ICacheInvalidationService invalidator,
        ISocketService socketService,
        ILogger<WorkspaceInvitationService>? logger = null)
        : IWorkspaceInvitationAppService // Bu interface'i oluşturman gerekecek
    {
        private readonly IRepository<WorkspaceInvitation> _invitationRepo = uow.Repository<WorkspaceInvitation>();
        private readonly IRepository<WorkspaceMember> _memberRepo = uow.Repository<WorkspaceMember>();
        private readonly IRepository<Workspace> _workspaceRepo = uow.Repository<Workspace>();
        private readonly IRepository<User> _userRepo = uow.Repository<User>();

        // ---------------------------------------------------------------------
        // 1. SEND INVITATION
        // ---------------------------------------------------------------------
        public async Task<Result<ReadWorkspaceInvitationDto>> SendInvitationAsync(Guid workspaceId, CreateWorkspaceInvitationDto dto, Guid actorId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {          
                var workspaceName = await _workspaceRepo.Query()
                    .AsNoTracking()
                    .Where(w => w.Id == workspaceId)
                    .Select(w => w.Name)
                    .FirstOrDefaultAsync(ct);

                var actorInfo = await _memberRepo.Query()
                    .AsNoTracking()
                    .Where(m => m.WorkspaceId == workspaceId && m.UserId == actorId)
                    .Select(m => new { m.Role, m.User.DisplayName })
                    .FirstOrDefaultAsync(ct);

                if (actorInfo != null)
                {
                    if (dto.Role >= actorInfo.Role)
                        throw new UnauthorizedAccessException("You cannot invite someone with a role equal to or higher than yours.");
                }

                if (dto.Role == WorkspaceRole.Owner)
                    throw new InvalidOperationException("You can't invite someone as a Workspace Owner. Use ownership transfer instead.");

                var targetStatus = await _userRepo.Query()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(u => u.Email == dto.Email)
                    .Select(u => new 
                    { 
                        UserId = u.Id,
                        UserIsDeleted = u.IsDeleted,
                        MemberInfo = _memberRepo.Query()
                            .IgnoreQueryFilters()
                            .Where(m => m.WorkspaceId == workspaceId && m.UserId == u.Id)
                            .Select(m => new { m.IsDeleted })
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync(ct);

                if (targetStatus != null)
                {
                    if (targetStatus.UserIsDeleted)
                        throw new InvalidOperationException("This user account has been deleted globally.");

                    if (targetStatus.MemberInfo != null && !targetStatus.MemberInfo.IsDeleted)
                         throw new InvalidOperationException("User is already an active member of this workspace.");
                }

                var pendingInvite = await _invitationRepo.Query()
                    .Where(i => i.WorkspaceId == workspaceId 
                                && i.Email == dto.Email 
                                && i.Status == InvitationStatus.Pending)
                    .FirstOrDefaultAsync(ct);

                if (pendingInvite != null)
                {
                    if (pendingInvite.ExpiresAt > DateTime.UtcNow)
                        throw new InvalidOperationException("A pending invitation already exists for this email.");
                    
                    pendingInvite.Status = InvitationStatus.Expired;
                }

                var newInvitation = new WorkspaceInvitation
                {
                    WorkspaceId = workspaceId,
                    InviterId = actorId,
                    InviterName = actorInfo?.DisplayName,
                    Email = dto.Email,
                    Role = dto.Role,
                    Status = InvitationStatus.Pending,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await _invitationRepo.AddAsync(newInvitation, ct);
                await uow.SaveChangesAsync(ct);

                var readDto = mapper.Map<ReadWorkspaceInvitationDto>(newInvitation);
                readDto.WorkspaceName = workspaceName;
                readDto.InviterName = actorInfo?.DisplayName ?? "Admin"; 

                if (targetStatus != null)
                {
                    await notificationService.SendNotificationAsync(
                        targetStatus.UserId,
                        "New Workspace Invitation",
                        $"You have been invited to join '{workspaceName}' as {dto.Role} by {readDto.InviterName}.",
                        ct);

                    await invalidator.InvalidateUserInvitations(targetStatus.UserId);
                    await socketService.SendToUserAsync(targetStatus.UserId, "WorkspaceInvitationReceived", readDto, ct);
                }

                return readDto;

            }, logger, "INVITATION_SEND_FAILED");
        }

        // ---------------------------------------------------------------------
        // 2. GET MY PENDING INVITATIONS
        // ---------------------------------------------------------------------
        public async Task<Result<PagedResult<ReadWorkspaceInvitationDto>>> GetMyInvitationsPagedAsync(
            Guid userId, 
            int page = 1, 
            int pageSize = 10, 
            CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                if (page < 1) page = 1;
                if (pageSize > 50) pageSize = 20; 

                var versionKey = CacheKeys.UserInvitationVersion(userId);
                var version = await cache.GetAsync<long>(versionKey); 
                if (version == 0) version = DateTime.UtcNow.Ticks;

                var cacheKey = CacheKeys.UserInvitationsPaged(userId, page, pageSize, version);

                var result = await cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var userEmail = await _userRepo.Query()
                            .AsNoTracking()
                            .Where(u => u.Id == userId)
                            .Select(u => u.Email)
                            .FirstOrDefaultAsync(ct);

                        if (string.IsNullOrEmpty(userEmail))
                            throw new InvalidOperationException("User email not found!");

                        var normalizedEmail = userEmail.ToLower();

                        var query = _invitationRepo.Query()
                            .AsNoTracking()
                            .Include(i => i.Workspace) 
                            .Where(i => i.Email.ToLower() == normalizedEmail 
                                        && i.Status == InvitationStatus.Pending 
                                        && i.ExpiresAt > DateTime.UtcNow);

                        var totalCount = await query.CountAsync(ct);

                        var items = await query
                            .OrderByDescending(i => i.CreatedAt)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync(ct);
                        
                        var dtos = mapper.Map<List<ReadWorkspaceInvitationDto>>(items);
                        return new PagedResult<ReadWorkspaceInvitationDto>(dtos, totalCount);
                    },
                    TimeSpan.FromMinutes(10)); 

                return result;

            }, logger, "INVITATION_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // 3. RESPOND (ACCEPT / DECLINE)
        // ---------------------------------------------------------------------
        public async Task<Result> RespondAsync(Guid invitationId, Guid userId, bool accept, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // 1. User & Invitation Fetch
                var user = await _userRepo.GetByIdAsync(userId, false, ct)
                        ?? throw new InvalidOperationException("User not found.");

                var invitation = await _invitationRepo.Query()
                    .Include(i => i.Workspace)
                    .FirstOrDefaultAsync(i => i.Id == invitationId, ct)
                    ?? throw new InvalidOperationException("Invitation not found.");

                // 2. Validations
                if (invitation.Email != user.Email)
                    throw new UnauthorizedAccessException("This invitation does not belong to you.");

                if (invitation.Status != InvitationStatus.Pending)
                    throw new InvalidOperationException($"Invitation is already {invitation.Status}.");

                if (invitation.ExpiresAt < DateTime.UtcNow)
                {
                    invitation.Status = InvitationStatus.Expired;
                    await uow.SaveChangesAsync(ct);
                    await invalidator.InvalidateUserInvitations(userId); 
                    throw new InvalidOperationException("Invitation has expired.");
                }

                // 3. DECLINE SCENARIO
                if (!accept)
                {
                    invitation.Status = InvitationStatus.Declined;
                    await uow.SaveChangesAsync(ct);

                    await notificationService.SendNotificationAsync(invitation.InviterId, "Workspace Invitation Declined", $"{user.DisplayName} declined your invitation.", ct);
                    await invalidator.InvalidateUserInvitations(userId);
                    return;
                }

                // 4. ACCEPT SCENARIO
                var existingMember = await _memberRepo.Query()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == invitation.WorkspaceId && m.UserId == userId, ct);

                if (existingMember != null && !existingMember.IsDeleted)
                {
                    invitation.Status = InvitationStatus.Accepted;
                    await uow.SaveChangesAsync(ct);
                    
                    await invalidator.InvalidateUserInvitations(userId);
                    return;
                }

                Guid activeMemberId; 

                if (existingMember != null)
                {
                    existingMember.IsDeleted = false;
                    existingMember.DeletedAt = null;
                    existingMember.DeletedBy = null;
                    existingMember.UpdatedAt = DateTime.UtcNow;
                    existingMember.Role = invitation.Role;
                    await _memberRepo.UpdateAsync(existingMember); 
                    
                    activeMemberId = existingMember.Id;
                }
                else
                {
                    var newMember = new WorkspaceMember
                    {
                        WorkspaceId = invitation.WorkspaceId,
                        UserId = userId,
                        Role = invitation.Role
                    };
                    await _memberRepo.AddAsync(newMember, ct);
                    
                    activeMemberId = newMember.Id; 
                }

                invitation.Status = InvitationStatus.Accepted;

                await uow.SaveChangesAsync(ct);

                // -------------------------------------------------------------
                // CACHE INVALIDATION
                // -------------------------------------------------------------
                await invalidator.InvalidateWorkspace(invitation.WorkspaceId);
                await invalidator.InvalidateUserInvitations(userId);
                await cache.RemoveAsync(CacheKeys.UserWorkspaces(userId));

                // -------------------------------------------------------------
                // PREPARE DTO FOR SOCKET 
                // -------------------------------------------------------------
                
                var finalMemberEntity = await _memberRepo.Query()
                    .AsNoTracking()
                    .Include(m => m.User) 
                    .FirstAsync(m => m.Id == activeMemberId, ct);

                var memberDto = mapper.Map<ReadWorkspaceMemberDto>(finalMemberEntity);

                // -------------------------------------------------------------
                // NOTIFY & SOCKET
                // -------------------------------------------------------------
                await notificationService.SendNotificationAsync(invitation.InviterId, "Workspace Invitation Accepted", $"{user.DisplayName} joined the workspace.", ct);
                await socketService.SendToWorkspaceAsync(invitation.WorkspaceId, "MemberAdded", memberDto, ct);

            }, logger, "INVITATION_RESPOND_FAILED");
        }
    }
}