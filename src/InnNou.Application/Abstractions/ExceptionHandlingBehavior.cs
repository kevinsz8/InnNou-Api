using InnNou.Application.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InnNou.Application.Abstractions
{
    public class ExceptionHandlingBehavior<TRequest, TResponse>
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
                // log here

                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    var responseType = typeof(TResponse).GetGenericArguments()[0];

                    var failureMethod = typeof(ApiResponse<>)
                        .MakeGenericType(responseType)
                        .GetMethod("FailureResponse", new[] { typeof(string), typeof(string), typeof(int?) });

                    var result = failureMethod!.Invoke(null, new object[]
                    {
                    "UNHANDLED_ERROR",
                    ex.Message,
                    500
                    });

                    return (TResponse)result!;
                }

                throw;
            }
        }
    }
}
