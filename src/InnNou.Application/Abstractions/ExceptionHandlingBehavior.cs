using InnNou.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InnNou.Application.Abstractions
{
    public class ExceptionHandlingBehavior<TRequest, TResponse>(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                // ApiException is an anticipated, already-classified failure (a semantic 4xx the
                // caller can act on) — logged at Warning so it doesn't drown out genuine unhandled
                // exceptions. Anything else reaching this catch-all is by definition unanticipated,
                // logged at Error with the full exception + request type. Previously this was a
                // bare "// log here" comment with no logging call at all — every unhandled
                // exception in the whole API vanished with zero trace.
                if (ex is ApiException loggedApiEx)
                    logger.LogWarning(ex, "{RequestType} failed with {ErrorCode}: {ErrorMessage}", typeof(TRequest).Name, loggedApiEx.Code, loggedApiEx.Message);
                else
                    logger.LogError(ex, "Unhandled exception in {RequestType}", typeof(TRequest).Name);

                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    var responseType = typeof(TResponse).GetGenericArguments()[0];

                    var failureMethod = typeof(ApiResponse<>)
                        .MakeGenericType(responseType)
                        .GetMethod("FailureResponse", new[] { typeof(string), typeof(string), typeof(int?) });

                    var (code, message, statusCode) = ex is ApiException apiEx
                        ? (apiEx.Code, apiEx.Message, apiEx.StatusCode)
                        : (ErrorCodes.UnhandledError, ex.Message, 500);

                    var result = failureMethod!.Invoke(null, new object[]
                    {
                    code,
                    message,
                    statusCode
                    });

                    return (TResponse)result!;
                }

                throw;
            }
        }
    }
}
