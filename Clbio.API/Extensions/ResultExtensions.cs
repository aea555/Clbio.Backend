// ResultExtensions.cs
using Clbio.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.Success)
            {
                return new OkObjectResult(ApiResponse.Ok(result.Value));
            }

            return new BadRequestObjectResult(ApiResponse.Fail(result.Error, result.Code));
        }
    }
}