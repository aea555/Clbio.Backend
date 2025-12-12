namespace Clbio.API.Extensions
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Code { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public static ApiResponse<T> Ok(T data) =>
            new() { Success = true, Data = data };
        public static ApiResponse<T> Fail(string error, string? code = null) =>
            new() { Success = false, Error = error, Code = code };
    }

    public class ApiResponse : ApiResponse<object?>
    {
        public static ApiResponse Ok(string message) =>
            new() { Success = true, Data = message };
    }
}
