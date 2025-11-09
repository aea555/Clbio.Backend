using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Extensions
{
    public static class SafeExecution
    {
        public static async Task<Result<TValue>> ExecuteSafeAsync<TValue>(
            Func<Task<TValue>> action,
            ILogger? logger = null,
            string errorCode = "SERVICE_ERROR")
        {
            try
            {
                return Result<TValue>.Ok(await action());
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error executing service action: {ErrorCode}", errorCode);
                return Result<TValue>.FromException(ex, errorCode);
            }
        }

        public static async Task<Result> ExecuteSafeAsync(
            Func<Task> action,
            ILogger? logger = null,
            string errorCode = "SERVICE_ERROR")
        {
            try
            {
                await action();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error executing service action: {ErrorCode}", errorCode);
                return Result.Fail(ex.Message, errorCode);
            }
        }
    }
}
