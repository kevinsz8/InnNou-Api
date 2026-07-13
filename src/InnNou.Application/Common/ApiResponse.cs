namespace InnNou.Application.Common
{
    // Lets cross-cutting MediatR pipeline behaviors (e.g. IdempotencyBehavior) inspect
    // Success/StatusCode/ReturnData on a closed ApiResponse<T> without reflection. ReturnData
    // is exposed as object? here (the generic T isn't expressible on a non-generic interface) —
    // System.Text.Json serializes an object?-typed member using its runtime type, so this is
    // enough for a behavior to cache/replay the payload without needing ApiResponse<T>'s
    // (private) constructor.
    public interface IApiResponse
    {
        bool Success { get; }
        int? StatusCode { get; }
        object? ReturnData { get; }
    }

    public class ApiResponse<T> : IApiResponse
    {
        public T? ReturnData { get; }
        public List<ApiError> Errors { get; }
        public bool Success { get; }
        public int? StatusCode { get; }
        public DateTime Timestamp { get; }

        object? IApiResponse.ReturnData => ReturnData;

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
