using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InnNou.Application.Abstractions
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Registration order matters — MediatR runs behaviors outermost-first. Idempotency
            // wraps the exception handler so a cache hit short-circuits before exception handling
            // even runs, and by the time control returns here after next(), ExceptionHandlingBehavior
            // has already normalized any thrown ApiException into a Success=false ApiResponse<T>.
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceExtension).Assembly));
            return services;
        }
    }
}
