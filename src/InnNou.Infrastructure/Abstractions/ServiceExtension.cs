using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Persistence;
using InnNou.Domain.Persistence;
using InnNou.Infrastructure.Mappers;
using InnNou.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace InnNou.Infrastructure.Abstractions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceExtension
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, ConfigurationManager configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddAutoMapper(options => options.AddProfile(typeof(MappingProfile)));

            var connectionString = configuration.GetConnectionString("InnNouConnection")
                ?? throw new InvalidOperationException("Connection string 'InnNouConnection' is missing.");

            services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

            services.AddScoped<IRequestContext, RequestContext>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IHotelService, HotelService>();
            services.AddScoped<IRoleService, RoleService>();

            return services;
        }
    }
}
