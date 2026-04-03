namespace InnNou.Application.Common
{
    public class ApiResponse<T>
    {
        public T? ReturnData { get; }
        public List<ApiError> Errors { get; }
        public bool Success { get; }
        public int? StatusCode { get; }
        public DateTime Timestamp { get; }

        private ApiResponse(T? data, bool success, List<ApiError>? errors = null, int? statusCode = null)
        {
            ReturnData = data;
            Success = success;
            Errors = errors ?? new();
            StatusCode = statusCode;
            Timestamp = DateTime.UtcNow;
        }

        public static ApiResponse<T> SuccessResponse(T data, int? statusCode = null)
            => new ApiResponse<T>(data, true, null, statusCode);

        public static ApiResponse<T> FailureResponse(string code, string description, int? statusCode = null)
            => new ApiResponse<T>(default, false, new List<ApiError> { new ApiError { Code = code, Description = description } }, statusCode);

        public static ApiResponse<T> FailureResponse(List<ApiError> errors, int? statusCode = null)
            => new ApiResponse<T>(default, false, errors, statusCode);
    }
}
