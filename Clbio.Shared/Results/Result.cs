namespace Clbio.Shared.Results
{
    public class Result
    {
        public bool Success { get; protected set; }
        public string? Error { get; protected set; }
        public string? Code { get; protected set; }

        public static Result Ok() => new() { Success = true };
        public static Result Fail(string error, string? code = null) =>
            new() { Success = false, Error = error, Code = code };

        public static Result FromException(Exception ex, string? code = "EXCEPTION") =>
            new() { Success = false, Error = ex.Message, Code = code };

        public override string ToString() =>
            Success ? "Success" : $"Error ({Code ?? "N/A"}): {Error}";
    }

    public class Result<T> : Result
    {
        public T? Value { get; protected set; }

        public static Result<T> Ok(T value) => new() { Success = true, Value = value };
        public new static Result<T> Fail(string error, string? code = null) =>
            new() { Success = false, Error = error, Code = code };

        public new static Result<T> FromException(Exception ex, string? code = "EXCEPTION") =>
            new() { Success = false, Error = ex.Message, Code = code };

        public static async Task<Result<T>> FromAsync(Func<Task<T>> func, string? code = "ASYNC_ERROR")
        {
            try
            {
                var result = await func();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return FromException(ex, code);
            }
        }

        public Result<K> Map<K>(Func<T, K> mapper)
        {
            if (!Success || Value == null)
                return Result<K>.Fail(Error ?? "Mapping failed", Code);

            try
            {
                return Result<K>.Ok(mapper(Value));
            }
            catch (Exception ex)
            {
                return Result<K>.FromException(ex);
            }
        }
    }
}
