using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InnNou.Application.Abstractions
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(options => options.AddProfile(typeof(MappingProfile)));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceExtension).Assembly));
            return services;
        }
    }
}
