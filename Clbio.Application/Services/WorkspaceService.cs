using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Workspace;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public sealed class WorkspaceService(
        IUnitOfWork uow,
        ICacheService cache,
        ICacheInvalidationService invalidator,
        ICacheVersionService versions,
        IMapper mapper,
        ILogger<WorkspaceService>? logger = null) : IWorkspaceAppService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IRepository<Workspace> _workspaceRepo = uow.Repository<Workspace>();
        private readonly IRepository<WorkspaceMember> _workspaceMemberRepo = uow.Repository<WorkspaceMember>();
        private readonly IRepository<User> _userRepo = uow.Repository<User>();
        private readonly IRepository<Board> _boardRepo = uow.Repository<Board>();

        private readonly ICacheService _cache = cache;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly ICacheVersionService _versions = versions;

        private readonly IMapper _mapper = mapper;
        private readonly ILogger<WorkspaceService>? _logger = logger;

        // ======================================================================
        // GET WORKSPACE BY ID
        // ======================================================================
        public async Task<Result<ReadWorkspaceDto?>> GetByIdAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var version = await _versions.GetWorkspaceVersionAsync(workspaceId);
                var key = CacheKeys.Workspace(workspaceId, version);

                var dto = await _cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        var entity = await _workspaceRepo.Query()
                            .AsNoTracking()
                            .Include(w => w.Owner) 
                            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

                        if (entity is null)
                            return null;

                        return _mapper.Map<ReadWorkspaceDto>(entity);
                    },
                    TimeSpan.FromMinutes(10));

                return dto;

            }, _logger, "WORKSPACE_READ_FAILED");
        }

        // ======================================================================
        // GET ALL WORKSPACES FOR USER
        // ======================================================================
        public async Task<Result<IEnumerable<ReadWorkspaceDto>>> GetAllForUserAsync(
            Guid userId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var userWsKey = CacheKeys.UserWorkspaces(userId);

                var workspaceIds = await _cache.GetOrSetAsync(
                    userWsKey,
                    async () =>
                    {
                        return await _workspaceMemberRepo.Query()
                            .AsNoTracking()
                            .Where(m => m.UserId == userId)
                            .Select(m => m.WorkspaceId)
                            .ToListAsync(ct);
                    },
                    TimeSpan.FromDays(1)); 

                if (workspaceIds == null || workspaceIds.Count == 0)
                    return Enumerable.Empty<ReadWorkspaceDto>();

                // Load versions in parallel
                var versionTasks = workspaceIds
                    .Select(id => _versions.GetWorkspaceVersionAsync(id))
                    .ToArray();
                var versions = await Task.WhenAll(versionTasks);

                // build cache keys
                var keys = workspaceIds
                    .Zip(versions, (id, ver) => CacheKeys.Workspace(id, ver))
                    .ToList();

                // batch GET from Redis
                var cachedValues = await _cache.GetManyAsync<ReadWorkspaceDto>(keys);

                // identify missing workspaces
                var missing = new List<Guid>();
                for (int i = 0; i < cachedValues.Count; i++)
                {
                    if (cachedValues[i] == null)
                        missing.Add(workspaceIds[i]);
                }

                // batch Db fetch for missing entries
                if (missing.Count > 0)
                {
                    var missingEntities = await _workspaceRepo.Query()
                        .AsNoTracking()
                        .Include(w => w.Owner)
                        .Where(w => missing.Contains(w.Id))
                        .ToListAsync(ct);

                    // batch SET missing entries into Redis
                    var setBatch = new Dictionary<string, ReadWorkspaceDto>();
                    foreach (var w in missingEntities)
                    {
                        var v = await _versions.GetWorkspaceVersionAsync(w.Id);
                        var dto = _mapper.Map<ReadWorkspaceDto>(w); 
                        setBatch[CacheKeys.Workspace(w.Id, v)] = dto;
                    }

                    if (setBatch.Count > 0)
                        await _cache.SetManyAsync(setBatch);

                    // update memory list
                    for (int i = 0; i < cachedValues.Count; i++)
                    {
                        if (cachedValues[i] == null)
                        {
                            var match = missingEntities.FirstOrDefault(w => w.Id == workspaceIds[i]);
                            if (match != null)
                                cachedValues[i] = _mapper.Map<ReadWorkspaceDto>(match);
                        }
                    }
                }

                return cachedValues.Where(w => w != null).Select(w => w!).AsEnumerable();
            }, _logger, "WORKSPACE_GET_ALL_FAILED");
        }

        // ======================================================================
        // CREATE WORKSPACE
        // ======================================================================
        public async Task<Result<ReadWorkspaceDto>> CreateAsync(Guid ownerId, CreateWorkspaceDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var owner = await _userRepo.GetByIdAsync(ownerId, false, ct)
                            ?? throw new InvalidOperationException("Owner not found");

                var workspace = _mapper.Map<Workspace>(dto);
                workspace.Status = WorkspaceStatus.Active;
                workspace.OwnerId = ownerId;

                var createdWorkspace = await _workspaceRepo.AddAsync(workspace, ct);
                await _uow.SaveChangesAsync(ct);

                // Add owner as member
                var membership = new WorkspaceMember
                {
                    WorkspaceId = createdWorkspace.Id,
                    UserId = createdWorkspace.OwnerId,
                    Role = WorkspaceRole.Owner
                };

                await _workspaceMemberRepo.AddAsync(membership, ct);
                await _uow.SaveChangesAsync(ct);

                // Map to DTO
                var readDto = _mapper.Map<ReadWorkspaceDto>(workspace);
                readDto.OwnerDisplayName = owner.DisplayName;

                return readDto;

            }, _logger, "WORKSPACE_CREATE_FAILED");
        }

        // ======================================================================
        // UPDATE WORKSPACE
        // ======================================================================
        public async Task<Result> UpdateAsync(Guid workspaceId, UpdateWorkspaceDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var entity = await _workspaceRepo.GetByIdAsync(workspaceId, true, ct)
                              ?? throw new InvalidOperationException("Workspace not found");

                _mapper.Map(dto, entity);

                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(workspaceId);

            }, _logger, "WORKSPACE_UPDATE_FAILED");
        }

        // ======================================================================
        // ARCHIVE
        // ======================================================================
        public async Task<Result> ArchiveAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var entity = await _workspaceRepo.GetByIdAsync(workspaceId, true, ct)
                              ?? throw new InvalidOperationException("Workspace not found");

                entity.Status = WorkspaceStatus.Archived;

                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(workspaceId);

            }, _logger, "WORKSPACE_ARCHIVE_FAILED");
        }

        // ======================================================================
        // DELETE WORKSPACE
        // ======================================================================
        public async Task<Result> DeleteAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var entity = await _workspaceRepo.GetByIdAsync(workspaceId, false, ct)
                              ?? throw new InvalidOperationException("Workspace not found");

                var boardIds = await _boardRepo.Query()
                    .Where(b => b.WorkspaceId == workspaceId)
                    .Select(b => b.Id)
                    .ToListAsync(ct);

                var utcNow = DateTime.UtcNow;

                if (boardIds.Count != 0)
                {
                    var columnIds = await uow.Repository<Column>().Query()
                        .Where(c => boardIds.Contains(c.BoardId))
                        .Select(c => c.Id)
                        .ToListAsync(ct);

                    if (columnIds.Count != 0)
                    {
                        var taskIds = await uow.Repository<TaskItem>().Query()
                            .Where(t => columnIds.Contains(t.ColumnId))
                            .Select(t => t.Id)
                            .ToListAsync(ct);

                        if (taskIds.Count != 0)
                        {
                            await uow.Repository<Comment>().Query()
                                .Where(c => taskIds.Contains(c.TaskId))
                                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.DeletedAt, utcNow), ct);

                            await uow.Repository<Attachment>().Query()
                                .Where(a => taskIds.Contains(a.TaskId))
                                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.DeletedAt, utcNow), ct);

                            await uow.Repository<TaskItem>().Query()
                                .Where(t => taskIds.Contains(t.Id))
                                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.DeletedAt, utcNow), ct);
                        }

                        await uow.Repository<Column>().Query()
                            .Where(c => columnIds.Contains(c.Id))
                            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.DeletedAt, utcNow), ct);
                    }

                    await _boardRepo.Query()
                        .Where(b => boardIds.Contains(b.Id))
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.DeletedAt, utcNow), ct);
                }

                await _workspaceMemberRepo.Query()
                    .Where(m => m.WorkspaceId == workspaceId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.DeletedAt, utcNow), ct);
                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(workspaceId);

            }, _logger, "WORKSPACE_DELETE_FAILED");
        }
    }
}
