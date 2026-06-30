using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Mapping;
using InnNou.Application.Persistence;
using InnNou.Domain.Persistence;
using InnNou.Infrastructure.Mapping;
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

            services.AddSingleton<InnNou.Shared.Mapping.IMapper>(_ =>
            {
                var mapper = new InnNou.Shared.Mapping.Mapper();
                ApplicationMappings.Register(mapper);
                InfrastructureMappings.Register(mapper);
                return mapper;
            });

            var connectionString = configuration.GetConnectionString("InnNouConnection")
                ?? throw new InvalidOperationException("Connection string 'InnNouConnection' is missing.");

            services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

            services.AddScoped<IRequestContext, RequestContext>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IHotelService, HotelService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ISupplierService, SupplierService>();

            // Catalog services
            services.AddScoped<IUnitTypeService, UnitTypeService>();
            services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
            services.AddScoped<IUnitConversionRateService, UnitConversionRateService>();
            services.AddScoped<IFamilyService, FamilyService>();
            services.AddScoped<ISubFamilyService, SubFamilyService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ISubCategoryService, SubCategoryService>();
            services.AddScoped<IHotelContactService, HotelContactService>();

            return services;
        }
    }
}
