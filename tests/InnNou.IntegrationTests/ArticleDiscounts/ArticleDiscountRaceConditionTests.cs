using Dapper;
using FluentAssertions;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Infrastructure.Abstractions;
using InnNou.IntegrationTests.TestFixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InnNou.IntegrationTests.ArticleDiscounts;

/// <summary>
/// Regression coverage for the TOCTOU race fixed 2026-08-07 (full-system audit finding #4):
/// ArticleDiscountService.EnsureNoOverlapAsync ran a SELECT-then-decide check in C# with no DB-level
/// atomicity, so two genuinely-concurrent CreateAsync calls for the identical scope+overlapping
/// dates could both pass the pre-check before either INSERT landed, producing two simultaneously
/// active overlapping discounts — exactly the state Create/Edit/SetActive are supposed to forbid.
/// Fixed by adding a DB-level backstop guard (sp_getapplock scoped to
/// "ArticleDiscountScope:{SupplierId}:{resolved scope}", re-checking overlap after acquiring the
/// lock) to sp_ArticleDiscount_Create/_Update/_SetActive — same "C# primary check, DB backstop"
/// shape as sp_StockLevel_ApplyDelta, adapted for an INSERT-vs-absence-of-conflict check rather
/// than a single-row UPDATE (which serializes for free via row locking under READ COMMITTED; this
/// does not). See CLAUDE.md's ArticleDiscountModule section and ParLevelOverrides' identical fix
/// (ParLevelRaceConditionTests) for the parallel case.
///
/// Deliberately does NOT inherit TransactionalTestBase — same reasoning as
/// OrderApprovalRaceConditionTests: an ambient TransactionScope would enlist both "concurrent"
/// calls into one shared transaction, which can never actually race for the same app lock the way
/// two independent real API requests do. Uses two independent DI scopes/connections instead, and
/// cleans up its own rows manually since there is no rollback safety net here.
/// </summary>
public class ArticleDiscountRaceConditionTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task ConcurrentCreate_ForTheSameScopeAndOverlappingDates_OnlyOneWinsAndTheLoserGetsACleanConflict()
    {
        var namePrefix = $"ARTDISCRACE{Guid.NewGuid():N}"[..20];
        Guid organizationToken;
        int organizationId;
        Guid supplierToken;

        using (var setupScope = fixture.Services.CreateScope())
        {
            var setupData = new TestDataBuilder(setupScope.ServiceProvider);
            var setupContext = (TestRequestContext)setupScope.ServiceProvider.GetRequiredService<IRequestContext>();

            (organizationToken, organizationId) = await setupData.GetAssociateOrganizationAsync();

            setupContext.RoleLevel = 80;
            setupContext.OrganizationId = organizationId;
            setupContext.OrganizationTypeCode = "ASSOCIATE";

            supplierToken = await setupData.CreateSupplierAsync(organizationToken, namePrefix);
        }

        try
        {
            var today = DateTime.UtcNow.Date;

            async Task<ApiResponse<CreateArticleDiscountCommandResponse>> CreateAsync()
            {
                using var scope = fixture.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var context = (TestRequestContext)scope.ServiceProvider.GetRequiredService<IRequestContext>();
                context.RoleLevel = 80;

                return await mediator.Send(new CreateArticleDiscountCommandRequest
                {
                    SupplierToken = supplierToken,
                    DiscountTypeCode = "PERCENTAGE",
                    DiscountValue = 10m,
                    EffectiveFrom = today
                });
            }

            var task1 = CreateAsync();
            var task2 = CreateAsync();
            var results = await Task.WhenAll(task1, task2);

            results.Count(r => r.Success).Should().Be(1, "exactly one of two genuinely-concurrent creates for the same scope+overlapping dates should win");

            var loser = results.Single(r => !r.Success);
            loser.StatusCode.Should().Be(409, "the loser must get a clean, anticipated conflict — never a raw 500");
            loser.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.ArticleDiscountOverlapping);

            // The real integrity check: exactly one active ArticleDiscount must exist for this
            // supplier-wide scope — never two, which is the state the race used to be able to
            // produce despite the C#-side pre-check.
            using var verifyScope = fixture.Services.CreateScope();
            var connectionFactory = verifyScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            await using var connection = connectionFactory.CreateConnection();
            var activeCount = await connection.QuerySingleAsync<int>(
                """
                SELECT COUNT(*) FROM ArticleDiscounts d
                JOIN Suppliers s ON s.SupplierId = d.SupplierId
                WHERE s.SupplierToken = @supplierToken AND d.IsActive = 1
                  AND d.ArticleId IS NULL AND d.SubFamilyId IS NULL AND d.FamilyId IS NULL
                """,
                new { supplierToken });
            activeCount.Should().Be(1);
        }
        finally
        {
            await CleanupAsync(supplierToken, organizationToken);
        }
    }

    private async Task CleanupAsync(Guid supplierToken, Guid organizationToken)
    {
        using var scope = fixture.Services.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            """
            DECLARE @SupplierId INT = (SELECT SupplierId FROM Suppliers WHERE SupplierToken = @supplierToken);
            DECLARE @OrganizationId INT = (SELECT OrganizationId FROM Organizations WHERE OrganizationToken = @organizationToken);

            DELETE FROM ArticleDiscounts WHERE SupplierId = @SupplierId;
            DELETE FROM OrganizationSuppliers WHERE SupplierId = @SupplierId;
            DELETE FROM Notifications WHERE UserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId);
            DELETE FROM AuditLogs WHERE ActorUserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId) OR EffectiveUserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId);
            DELETE FROM Users WHERE SupplierId = @SupplierId;
            DELETE FROM Suppliers WHERE SupplierId = @SupplierId;
            """,
            new { supplierToken, organizationToken });
    }
}
