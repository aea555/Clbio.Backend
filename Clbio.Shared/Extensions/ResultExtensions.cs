using Clbio.Shared.Results;

namespace Clbio.Shared.Extensions
{
    public static class ResultExtensions
    {
        public static async Task<Result> ToResult(this Task<Result<object>> task)
        {
            var result = await task;
            return result.Success ? Result.Ok() : Result.Fail(result.Error ?? "UNKNOWN_ERROR", result.Code);
        }
    }
}
