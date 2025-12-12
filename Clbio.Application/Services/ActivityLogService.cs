using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.DTOs.V1.ActivityLog;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class ActivityLogService(
        IServiceScopeFactory scopeFactory,
        IUnitOfWork uow,
        IMapper mapper,
        ILogger<ActivityLogService>? logger = null)
        : ServiceBase<ActivityLog>(uow, logger), IActivityLogAppService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IMapper _mapper = mapper;
        private readonly IRepository<ActivityLog> _logRepo = uow.Repository<ActivityLog>();

        // read only service
        public async Task<Result<(IEnumerable<ReadActivityLogDto> Items, int TotalCount)>> GetPagedAsync(
            Guid workspaceId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // filter based on workspace
                var query = _logRepo.Query()
                    .Where(l => l.WorkspaceId == workspaceId)
                    .Include(l => l.Actor) // done by
                    .OrderByDescending(l => l.CreatedAt);

                var total = await query.CountAsync(ct);

                // pagination
                var entities = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                var dtos = _mapper.Map<IEnumerable<ReadActivityLogDto>>(entities);

                return (dtos, total);

            }, _logger, "ACTIVITY_LOG_LIST_FAILED");
        }

        public Task LogAsync(Guid workspaceId, Guid userId, string entityType, Guid entityId, string actionType, string metadata, CancellationToken ct = default)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var logRepo = uow.Repository<ActivityLog>();


                    var log = new ActivityLog
                    {
                        WorkspaceId = workspaceId,
                        ActorId = userId,
                        EntityType = entityType,
                        EntityId = entityId,
                        ActionType = actionType,
                        Metadata = metadata,
                        CreatedAt = DateTime.UtcNow
                    };

                    await logRepo.AddAsync(log, CancellationToken.None);
                    await uow.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create activity log");
                }
            }, CancellationToken.None);
            return Task.CompletedTask;
        }
    }
}