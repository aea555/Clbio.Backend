using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.DTOs.V1.User;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;

namespace Clbio.Application.Services
{
    public class UserService(
        IUnitOfWork uow,
        IMapper mapper,
        ICacheInvalidationService invalidator,
        Microsoft.Extensions.Logging.ILogger<UserService>? logger = null)
        : ServiceBase<User>(uow, logger), IUserAppService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly IRepository<User> _userRepo = uow.Repository<User>();

        public async Task<Result<ReadUserDto?>> GetAsync(Guid userId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var user = await _userRepo.GetByIdAsync(userId, false, ct);
                if (user == null) return null;

                return _mapper.Map<ReadUserDto>(user);
            }, _logger, "USER_GET_FAILED");
        }

        public async Task<Result> UpdateAsync(Guid userId, UpdateUserDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // find user
                var user = await _userRepo.GetByIdAsync(userId, true, ct)
                           ?? throw new InvalidOperationException("User not found.");

                _mapper.Map(dto, user);

                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateUser(userId);

            }, _logger, "USER_UPDATE_FAILED");
        }
    }
}