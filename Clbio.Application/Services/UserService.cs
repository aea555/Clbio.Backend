using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Infrastructure;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.DTOs.V1.User;
using Clbio.Application.Extensions;
using Clbio.Application.Helpers;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace Clbio.Application.Services
{
    public class UserService(
        IUnitOfWork uow,
        IMapper mapper,
        ICacheService cache,
        ICacheInvalidationService invalidator,
        IFileStorageService fileStorage,
        Microsoft.Extensions.Logging.ILogger<UserService>? logger = null)
        : ServiceBase<User>(uow, logger), IUserAppService
    {
        private readonly IRepository<User> _userRepo = uow.Repository<User>();

        public async Task<Result<ReadUserDto?>> GetAsync(Guid userId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var key = CacheKeys.User(userId);
                var userDto = await cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        var user = await _userRepo.GetByIdAsync(userId, false, ct);
                        if (user == null) return null;

                        return new ReadUserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            DisplayName = user.DisplayName,
                            AvatarUrl = user.AvatarUrl
                        };

                    },
                TimeSpan.FromHours(1));

                if (userDto == null) return null;

                if (!string.IsNullOrEmpty(userDto.AvatarUrl))
                {
                    userDto.AvatarUrl = fileStorage.GetPresignedUrl(userDto.AvatarUrl);
                }

                return userDto;
            }, _logger, "USER_GET_FAILED");
        }

        public async Task<Result> UpdateAsync(Guid userId, UpdateUserDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // find user
                var user = await _userRepo.GetByIdAsync(userId, true, ct)
                    ?? throw new InvalidOperationException("User not found.");

                mapper.Map(dto, user);

                await _uow.SaveChangesAsync(ct);

                await invalidator.InvalidateUser(userId);

            }, _logger, "USER_UPDATE_FAILED");
        }

        public async Task<Result<string>> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // 1. Validasyonlar
                if (file == null || file.Length == 0)
                    throw new InvalidOperationException("File is empty.");

                if (file.Length > 5 * 1024 * 1024) // 5 MB Limit 
                    throw new InvalidOperationException("Avatar size cannot exceed 5MB.");

                if (!FileValidationHelper.IsImage(file)) // 
                    throw new InvalidOperationException("Invalid file type. Only JPG, PNG and WebP are allowed.");

                var user = await _userRepo.GetByIdAsync(userId, true, ct)
                    ?? throw new InvalidOperationException("User not found.");

                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    try
                    {
                        await fileStorage.DeleteAsync(user.AvatarUrl, ct);
                    }
                    catch
                    {
                        // omit
                    }
                }

                var folderPath = $"users/{userId}";

                using var stream = file.OpenReadStream();
                var newUrl = await fileStorage.UploadAsync(stream, file.FileName, file.ContentType, folderPath, ct);

                user.AvatarUrl = newUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _uow.SaveChangesAsync(ct);
                await invalidator.InvalidateUser(userId);

                return fileStorage.GetPresignedUrl(newUrl);

            }, _logger, "AVATAR_UPLOAD_FAILED");
        }

        public async Task<Result> RemoveAvatarAsync(Guid userId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var user = await _userRepo.GetByIdAsync(userId, true, ct)
                    ?? throw new InvalidOperationException("User not found.");

                if (string.IsNullOrEmpty(user.AvatarUrl))
                    return;

                // delete
                try
                {
                    await fileStorage.DeleteAsync(user.AvatarUrl, ct);
                }
                catch
                {

                }

                user.AvatarUrl = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _uow.SaveChangesAsync(ct);
                await invalidator.InvalidateUser(userId);

            }, _logger, "AVATAR_REMOVE_FAILED");
        }
    }
}