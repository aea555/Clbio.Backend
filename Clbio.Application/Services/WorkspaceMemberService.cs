using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
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
    public class WorkspaceMemberService(
        IUnitOfWork uow,
        INotificationAppService notificationService,
        IMapper mapper,
        ICacheInvalidationService invalidator,
        ISocketService socketService,
        ILogger<IWorkspaceMemberAppService>? logger = null)
        : IWorkspaceMemberAppService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly ILogger<IWorkspaceMemberAppService>? _logger = logger;
        private readonly INotificationAppService _notificationService = notificationService;
        private readonly IMapper _mapper = mapper;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly ISocketService _socketService = socketService;

        private readonly IRepository<WorkspaceMember> _memberRepo = uow.Repository<WorkspaceMember>();
        private readonly IRepository<User> _userRepo = uow.Repository<User>();
        private readonly IRepository<Workspace> _workspaceRepo = uow.Repository<Workspace>();

        public async Task<Result<List<ReadWorkspaceMemberDto>>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var members = await _memberRepo.Query()
                    .Where(m => m.WorkspaceId == workspaceId)
                    .Include(m => m.User)
                    .ToListAsync(ct);
                return _mapper.Map<List<ReadWorkspaceMemberDto>>(members);
            }, _logger, "MEMBER_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // ADD MEMBER 
        // ---------------------------------------------------------------------
        public async Task<Result<ReadWorkspaceMemberDto>> AddMemberAsync(Guid workspaceId, CreateWorkspaceMemberDto dto, Guid actorId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var actor = await _userRepo.GetByIdAsync(actorId, false, ct)
                            ?? throw new InvalidOperationException($"Actor not found");

                // 1. find by email
                var targetUser = await _userRepo.Query()
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.EmailVerified, ct)
                    ?? throw new InvalidOperationException($"User with email '{dto.Email}' not found or is not verified.");

                var workspaceName = await _workspaceRepo.Query()
                    .Where(w => w.Id == workspaceId)
                    .Select(w => w.Name)
                    .FirstOrDefaultAsync(ct);

                // 2. already a member?
                var exists = await _memberRepo.Query()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUser.Id, ct);

                if (exists) throw new InvalidOperationException("User is already a member.");

                // 3. add
                var member = new WorkspaceMember
                {
                    WorkspaceId = workspaceId,
                    UserId = targetUser.Id,
                    Role = dto.Role
                };

                await _memberRepo.AddAsync(member, ct);
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateMembership(targetUser.Id, workspaceId);
                await _invalidator.InvalidateWorkspace(workspaceId);

                // 4. Return & Notify
                var createdMember = await _memberRepo.Query()
                    .Include(m => m.User)
                    .FirstAsync(m => m.Id == member.Id, ct);

                var readDto = _mapper.Map<ReadWorkspaceMemberDto>(createdMember);

                await _notificationService.SendNotificationAsync(
                    targetUser.Id,
                    "Workspace Invitation",
                    $"{actor?.DisplayName} added you to workspace '{workspaceName}' as {dto.Role}.",
                    ct);

                await _socketService.SendToWorkspaceAsync(workspaceId, "MemberAdded", readDto, ct);
                await _socketService.SendToUserAsync(targetUser.Id, "WorkspaceInvitationReceived", new { workspaceId }, ct);

                return readDto;
            }, _logger, "MEMBER_ADD_FAILED");
        }

        // ---------------------------------------------------------------------
        // UPDATE ROLE (hierarchy control)
        // ---------------------------------------------------------------------
        public async Task<Result<ReadWorkspaceMemberDto>> UpdateRoleAsync(Guid workspaceId, Guid targetUserId, WorkspaceRole newRole, Guid actorUserId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                if (targetUserId == actorUserId)
                    throw new InvalidOperationException("You cannot change your own role.");

                var workspaceName = await _workspaceRepo.Query()
                    .Where(w => w.Id == workspaceId)
                    .Select(w => w.Name)
                    .FirstOrDefaultAsync(ct);

                // fetch actor and target
                var (actorMember, targetMember, actorUser) = await GetActorAndTargetAsync(workspaceId, actorUserId, targetUserId, ct);

                // --- HIERARCHY CHECK ---
                // Global Admin Check
                if (actorUser.GlobalRole != GlobalRole.Admin)
                {
                    // 1. does actor have the permission relative to the target?
                    CheckHierarchy(actorMember!.Role, targetMember!.Role, "update role of");

                    // 2. The newly assigned role cannot be higher than or equal to the Actor's role (except for Owner).
                    // so a PrivilegedMember can't assign Owner role.
                    if (actorMember.Role != WorkspaceRole.Owner && newRole >= actorMember.Role)
                        throw new UnauthorizedAccessException("You cannot assign a role equal to or higher than your own.");

                    // 3. Ownership transfer will be through a different path
                    if (newRole == WorkspaceRole.Owner)
                        throw new InvalidOperationException("Ownership cannot be transferred via role update.");
                }

                // apply
                targetMember.Role = newRole;
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateMembership(targetUserId, workspaceId);

                var readDto = _mapper.Map<ReadWorkspaceMemberDto>(targetMember);

                if (targetUserId != actorUserId)
                {
                    await _notificationService.SendNotificationAsync(
                        targetUserId,
                        "Role Updated",
                        $"{actorUser.DisplayName} changed your role in '{workspaceName}' to {newRole}.",
                        ct);
                }

                await _socketService.SendToWorkspaceAsync(workspaceId, "MemberUpdated", readDto, ct);

                return readDto;

            }, _logger, "MEMBER_ROLE_UPDATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // REMOVE MEMBER (kick)
        // ---------------------------------------------------------------------
        public async Task<Result> RemoveMemberAsync(Guid workspaceId, Guid targetUserId, Guid actorUserId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                if (targetUserId == actorUserId)
                    throw new InvalidOperationException("Use 'Leave' endpoint to remove yourself.");

                var (actorMember, targetMember, actorUser) = await GetActorAndTargetAsync(workspaceId, actorUserId, targetUserId, ct);

                if (actorUser.GlobalRole != GlobalRole.Admin)
                {
                    CheckHierarchy(actorMember!.Role, targetMember!.Role, "remove");
                }

                var workspaceName = await _workspaceRepo.Query()
                    .Where(w => w.Id == workspaceId)
                    .Select(w => w.Name)
                    .FirstOrDefaultAsync(ct);

                // remove
                await PerformRemove(workspaceId, targetMember, ct);

                if (targetUserId != actorUserId)
                {
                    await _notificationService.SendNotificationAsync(
                        targetUserId,
                        "Removed from Workspace",
                        $"{actorUser.DisplayName} removed you from workspace '{workspaceName}'.",
                        ct);
                }

                // notify the user they have been KICKED OUT
                await _socketService.SendToUserAsync(targetUserId, "RemovedFromWorkspace", new { workspaceId }, ct);

            }, _logger, "MEMBER_REMOVE_FAILED");
        }

        // ---------------------------------------------------------------------
        // LEAVE WORKSPACE 
        // ---------------------------------------------------------------------
        public async Task<Result> LeaveWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var member = await _memberRepo.Query()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct)
                    ?? throw new InvalidOperationException("You are not a member of this workspace.");

                // Owner check: sole owner can't leave, they have to transfer ownership or delete the workspace
                if (member.Role == WorkspaceRole.Owner)
                {
                    // any co-owners?
                    var otherOwners = await _memberRepo.Query()
                        .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId != userId && m.Role == WorkspaceRole.Owner, ct);

                    if (!otherOwners)
                        throw new InvalidOperationException("As the sole owner, you cannot leave the workspace. Delete the workspace or transfer ownership first.");
                }

                await PerformRemove(workspaceId, member, ct);

            }, _logger, "MEMBER_LEAVE_FAILED");
        }

        // ---------------------------------------------------------------------
        // PRIVATE HELPERS 
        // ---------------------------------------------------------------------

        private async Task<(WorkspaceMember? Actor, WorkspaceMember Target, User ActorUser)> GetActorAndTargetAsync(Guid workspaceId, Guid actorId, Guid targetId, CancellationToken ct)
        {
            var actorUser = await _userRepo.GetByIdAsync(actorId, false, ct)
                            ?? throw new InvalidOperationException("Actor user not found.");

            var targetUser = await _userRepo.GetByIdAsync(targetId, false, ct)
                            ?? throw new InvalidOperationException("Target user not found.");

            // Check actor membership
            var actorMember = await _memberRepo.Query()
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == actorId, ct);

            // if not global admin, require membership
            if (actorUser.GlobalRole != GlobalRole.Admin && actorMember == null)
                throw new UnauthorizedAccessException("You are not a member of this workspace.");

            // target user
            var targetMember = await _memberRepo.Query()
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetId, ct)
                ?? throw new InvalidOperationException("Target member not found.");

            return (actorMember, targetMember, actorUser);
        }

        private static void CheckHierarchy(WorkspaceRole actorRole, WorkspaceRole targetRole, string action)
        {
            // Ruke: Actor's role most be greater than Target's role.
            // Owner(2) > Privileged(1) -> OK
            // Privileged(1) > Member(0) -> OK
            // Privileged(1) > Privileged(1) -> FAIL (Can't manage equals)
            // Member(0) > ... -> FAIL

            if (actorRole <= targetRole)
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action} a member with role '{targetRole}'.");
            }
        }

        private async Task PerformRemove(Guid workspaceId, WorkspaceMember member, CancellationToken ct)
        {
            await _memberRepo.DeleteAsync(member.Id, ct);
            await _uow.SaveChangesAsync(ct);
            await _invalidator.InvalidateMembership(member.UserId, workspaceId);
            await _invalidator.InvalidateWorkspace(workspaceId);

            // announce removal
            await _socketService.SendToWorkspaceAsync(workspaceId, "MemberRemoved", new { userId = member.UserId }, ct);
        }
    }
}