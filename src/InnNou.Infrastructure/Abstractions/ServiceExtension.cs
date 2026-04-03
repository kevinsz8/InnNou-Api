using InnNou.Domain.Persistence;
using InnNou.Infrastructure.Mappers;
using InnNou.Infrastructure.Repositories.DbContexts;
using InnNou.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
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

            services.AddDbContext<InnNouDbContext>(opt =>
            {
                opt.UseSqlServer(configuration.GetConnectionString("InnNouConnection"));
            });

            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
