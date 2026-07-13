using InnNou.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration; // IConfiguration + string indexer only — no ConfigurationBinder/GetValue<T> needed
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InnNou.Application.Abstractions
{
    // Opt-in idempotency for Command requests: only activates when the caller sends an
    // "Idempotency-Key" header, and only for requests whose type name identifies them as a
    // Command (see IsEligible). Absent the header, or on a Query, this is a no-op — every
    // existing request keeps behaving exactly as it did before this behavior existed.
    public class IdempotencyBehavior<TRequest, TResponse>(
        IIdempotencyStore store,
        IHttpContextAccessor httpContextAccessor,
        IRequestContext requestContext,
        IConfiguration configuration)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const string IdempotencyKeyHeader = "Idempotency-Key";

        // Every Command request in this codebase ends in "CommandRequest" (confirmed against
        // every Requests/*.cs file); every Query ends in "QueryRequest" and is excluded for free.
        // These two DO end in "CommandRequest" but are session/auth flows, not business-entity
        // mutations — retrying a login with the same key should never replay a stale token.
        private static readonly HashSet<string> DenylistedRequestNames = new(StringComparer.Ordinal)
        {
            "LoginCommandRequest",
            "StopImpersonateCommandRequest"
        };

        private sealed class CachedPayload
        {
            public JsonElement ReturnData { get; set; }
            public int? StatusCode { get; set; }
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestTypeName = typeof(TRequest).Name;

            if (!IsEligible(requestTypeName))
                return await next();

            var key = httpContextAccessor.HttpContext?.Request.Headers[IdempotencyKeyHeader].ToString();

            if (string.IsNullOrWhiteSpace(key))
                return await next();

            var requestHash = ComputeHash(request);
            var userToken = requestContext.ActorUserToken;
            var now = DateTime.UtcNow;
            var ttlHours = int.TryParse(configuration["Idempotency:TtlHours"], out var configuredTtlHours) ? configuredTtlHours : 24;

            var begin = await store.TryBeginAsync(key, requestTypeName, userToken, requestHash, now, now.AddHours(ttlHours), cancellationToken);

            switch (begin.Outcome)
            {
                case IdempotencyOutcome.Completed:
                    return BuildCachedResponse(begin.ResponseBody!);

                case IdempotencyOutcome.Pending:
                    throw new ApiException(ErrorCodes.IdempotencyRequestInProgress,
                        "A request with this idempotency key is already being processed.", 409);

                case IdempotencyOutcome.HashMismatch:
                    throw new ApiException(ErrorCodes.IdempotencyKeyPayloadMismatch,
                        "This idempotency key was already used with a different request payload.", 422);
            }

            try
            {
                var response = await next();

                if (response is IApiResponse { Success: true } apiResponse)
                {
                    var payload = JsonSerializer.Serialize(new CachedPayload
                    {
                        ReturnData = JsonSerializer.SerializeToElement(apiResponse.ReturnData),
                        StatusCode = apiResponse.StatusCode
                    });

                    await store.CompleteAsync(key, requestTypeName, userToken, apiResponse.StatusCode, payload, DateTime.UtcNow, cancellationToken);
                }
                else
                {
                    await store.ReleaseAsync(key, requestTypeName, userToken, cancellationToken);
                }

                return response;
            }
            catch
            {
                await store.ReleaseAsync(key, requestTypeName, userToken, cancellationToken);
                throw;
            }
        }

        private static bool IsEligible(string requestTypeName) =>
            requestTypeName.EndsWith("CommandRequest", StringComparison.Ordinal)
            && !DenylistedRequestNames.Contains(requestTypeName);

        private static string ComputeHash(TRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        // TResponse is ApiResponse<TInner> for every eligible request (Commands always return
        // that), but TInner is only known via reflection here — mirrors ExceptionHandlingBehavior's
        // existing use of MakeGenericType/GetMethod to construct a closed ApiResponse<TInner>
        // generically, just calling the SuccessResponse factory instead of FailureResponse.
        private static TResponse BuildCachedResponse(string cachedBody)
        {
            var cached = JsonSerializer.Deserialize<CachedPayload>(cachedBody)!;
            var innerType = typeof(TResponse).GetGenericArguments()[0];
            var data = cached.ReturnData.Deserialize(innerType);

            var successMethod = typeof(ApiResponse<>)
                .MakeGenericType(innerType)
                .GetMethod("SuccessResponse", new[] { innerType, typeof(int?) });

            var result = successMethod!.Invoke(null, new object?[] { data, cached.StatusCode });

            return (TResponse)result!;
        }
    }
}
