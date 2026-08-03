using InnNou.Application.Abstractions;
using InnNou.Application.Common;
using InnNou.Infrastructure.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InnNou.IntegrationTests.TestFixtures;

/// <summary>
/// Builds the real DI container - the exact same <c>AddInfrastructure()</c>/<c>AddApplication()</c>
/// calls <c>Program.cs</c> makes - pointed at InnNou_Test instead of the dev database. This is
/// the whole point of testing at this layer: real handlers, real mapper registrations, real SPs
/// against a real SQL Server, no mocking anything that could silently diverge from production.
///
/// Built once and shared across every test in a collection (see <see cref="TransactionalTestBase"/>) -
/// building the container is cheap, but there's no reason to redo it per test.
/// </summary>
public class DatabaseFixture
{
    public IServiceProvider Services { get; }

    public DatabaseFixture()
    {
        var configuration = new ConfigurationManager();
        configuration.AddJsonFile("appsettings.test.json", optional: false);
        // Lets a dev/CI override the connection string without touching the checked-in file,
        // e.g. INNOU_TEST_ConnectionStrings__InnNouConnection=... .
        configuration.AddEnvironmentVariables(prefix: "INNOU_TEST_");

        var services = new ServiceCollection();

        // IdempotencyBehavior<,> depends on IConfiguration and IHttpContextAccessor even outside
        // a real HTTP request/host - Program.cs gets both for free from WebApplication's own
        // builder.Services, but a plain ServiceCollection needs them registered explicitly.
        // Several Infrastructure services (e.g. OrderService) also take an ILogger<T>.
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpContextAccessor();
        services.AddLogging();

        services.AddInfrastructure(configuration);
        services.AddApplication();

        // Overrides AddInfrastructure()'s own IRequestContext registration (which reads JWT
        // claims off HttpContext) - last registration wins for GetService<IRequestContext>().
        services.AddScoped<IRequestContext, TestRequestContext>();

        Services = services.BuildServiceProvider();
    }
}
