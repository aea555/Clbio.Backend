using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.DTOs.V1.Role;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class RoleService(
        IUnitOfWork uow,
        IMapper mapper,
        ILogger<RoleService>? logger = null)
        : ServiceBase<RoleEntity>(uow, logger), IRoleAppService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IRepository<RoleEntity> _roleRepo = uow.Repository<RoleEntity>();

        public async Task<Result<List<ReadRoleDto>>> GetWorkspaceRolesAsync(CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var roles = await _roleRepo.Query()
                    .Where(r => r.WorkspaceRole != null) // workspace roles only
                    .OrderBy(r => r.WorkspaceRole)
                    .ToListAsync(ct);

                var dtos = roles.Select(r => new ReadRoleDto
                {
                    Id = r.Id,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    WorkspaceRoleValue = (int?)r.WorkspaceRole
                }).ToList();

                return dtos;

            }, _logger, "ROLE_LIST_FAILED");
        }
    }
}