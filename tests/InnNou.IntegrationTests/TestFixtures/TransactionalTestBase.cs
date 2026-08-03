using InnNou.Application.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;
using Xunit;

namespace InnNou.IntegrationTests.TestFixtures;

/// <summary>
/// Base class for integration tests. Wraps every test in a <see cref="TransactionScope"/> that's
/// never completed, so every Dapper connection opened during the test (by any service, at any
/// depth) auto-enlists in the ambient transaction and the whole thing rolls back on dispose -
/// nothing a test creates ever persists in InnNou_Test, regardless of how many separate
/// connections/SP calls it took to create it.
///
/// <see cref="TransactionScopeAsyncFlowOption.Enabled"/> is required - without it, the ambient
/// transaction does not flow across <c>await</c> boundaries, and every Dapper call in this
/// entirely-async codebase would silently run outside the scope.
/// </summary>
public abstract class TransactionalTestBase : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private TransactionScope? _transaction;
    private IServiceScope _scope = null!;

    protected TransactionalTestBase(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        _scope = _fixture.Services.CreateScope();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        _transaction?.Dispose(); // never Complete()'d -> rolls back everything the test did
        return Task.CompletedTask;
    }

    protected IMediator Mediator => _scope.ServiceProvider.GetRequiredService<IMediator>();

    /// <summary>The mutable request context for this test's scope - set RoleLevel/OrganizationId/
    /// etc. on it before sending a request, exactly like impersonating a specific user by hand.</summary>
    protected TestRequestContext Context => (TestRequestContext)_scope.ServiceProvider.GetRequiredService<IRequestContext>();

    protected TestDataBuilder Data => new(_scope.ServiceProvider);
}
