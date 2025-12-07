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
        IBoardService boardService,
        IMapper mapper,
        ILogger<WorkspaceService>? logger = null) : IWorkspaceAppService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IRepository<Workspace> _workspaceRepo = uow.Repository<Workspace>();
        private readonly IRepository<WorkspaceMember> _workspaceMemberRepo = uow.Repository<WorkspaceMember>();
        private readonly IRepository<User> _userRepo = uow.Repository<User>();
        private readonly IRepository<Board> _boardRepo = uow.Repository<Board>();

        private readonly IBoardService _boardService = boardService;

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

                var entity = await _cache.GetOrSetAsync(
                    CacheKeys.Workspace(workspaceId, version),
                    async () => await _workspaceRepo.GetByIdAsync(workspaceId, false, ct),
                    TimeSpan.FromMinutes(10));

                if (entity is null)
                    return null;

                var dto = _mapper.Map<ReadWorkspaceDto>(entity);
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
                var memberships = await _workspaceMemberRepo.Query()
                    .Where(m => m.UserId == userId)
                    .ToListAsync(ct);

                var workspaceIds = memberships.Select(m => m.WorkspaceId).ToList();

                if (workspaceIds.Count == 0)
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
                var cachedValues = await _cache.GetManyAsync<Workspace>(keys);

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
                        .Where(w => missing.Contains(w.Id))
                        .ToListAsync(ct);

                    // batch SET missing entries into Redis
                    var setBatch = new Dictionary<string, Workspace>();
                    foreach (var w in missingEntities)
                    {
                        var v = await _versions.GetWorkspaceVersionAsync(w.Id);
                        setBatch[CacheKeys.Workspace(w.Id, v)] = w;
                    }

                    await _cache.SetManyAsync(setBatch);

                    // replace missing cached values
                    for (int i = 0; i < cachedValues.Count; i++)
                    {
                        if (cachedValues[i] == null)
                        {
                            cachedValues[i] = missingEntities.FirstOrDefault(w => w.Id == workspaceIds[i]);
                        }
                    }
                }

                // convert to DTOs
                var result = cachedValues
                    .Where(w => w != null)
                    .Select(w => _mapper.Map<ReadWorkspaceDto>(w!))
                    .ToList();

                return result.AsEnumerable();
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

                await _workspaceRepo.AddAsync(workspace, ct);
                await _uow.SaveChangesAsync(ct);

                // Add owner as member
                var membership = new WorkspaceMember
                {
                    WorkspaceId = workspace.Id,
                    UserId = workspace.OwnerId,
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

                // Delete boards first
                var boards = await _boardRepo.FindAsync(b => b.WorkspaceId == workspaceId, true, ct);
                foreach (var board in boards)
                    await _boardService.DeleteAsync(board.Id, ct);

                await _workspaceRepo.DeleteAsync(workspaceId, ct);
                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(workspaceId);

            }, _logger, "WORKSPACE_DELETE_FAILED");
        }
    }
}
