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
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class ActivityLogService(
        IUnitOfWork uow,
        IMapper mapper,
        ILogger<ActivityLogService>? logger = null)
        : ServiceBase<ActivityLog>(uow, logger), IActivityLogAppService
    {
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
    }
}