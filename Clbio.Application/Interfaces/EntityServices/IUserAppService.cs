using Clbio.Application.DTOs.V1.User;
using Clbio.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IUserAppService
    {
        // get me
        Task<Result<ReadUserDto?>> GetAsync(Guid userId, CancellationToken ct = default);
        // update profile
        Task<Result> UpdateAsync(Guid userId, UpdateUserDto dto, CancellationToken ct = default);
        Task<Result<string>> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken ct = default);
        Task<Result> RemoveAvatarAsync(Guid userId, CancellationToken ct = default);
    }
}