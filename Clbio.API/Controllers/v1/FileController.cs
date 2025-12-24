using Clbio.Abstractions.Interfaces.Infrastructure;
using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}")]
    [Authorize]

    public class FileController(IFileStorageService fileStorage) : ControllerBase
    {
        [HttpGet("files/view/{*key}")]
        [RequirePermission(Permission.ViewAttachment, "workspaceId")]
        public async Task<IActionResult> GetFile(string key)
        {
            var signedUrl = fileStorage.GetPresignedUrl(key, durationMinutes: 60);
            return Ok(ApiResponse.Ok(new {url = signedUrl}));
        }
    }
}
