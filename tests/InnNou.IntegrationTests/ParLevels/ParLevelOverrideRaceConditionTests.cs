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

namespace InnNou.IntegrationTests.ParLevels;

/// <summary>
/// Regression coverage for the TOCTOU race fixed 2026-08-07 (full-system audit finding #4,
/// applied to Par Levels as the second half of the "fix it on both sides" instruction — see
/// ArticleDiscountRaceConditionTests for the sibling fix and full rationale):
/// ParLevelService.CreateOverrideAsync ran a SELECT-then-decide overlap check in C# with no
/// DB-level atomicity, so two genuinely-concurrent creates for the same (Warehouse, Article, Type)
/// scope with overlapping dates could both pass the pre-check before either INSERT landed.
/// Fixed by wrapping a second, DB-lock-protected re-check (same TryFindOverlap helper as the
/// pre-check, so the two can never drift) in a transaction guarded by sp_getapplock scoped to
/// "ParLevelOverrideScope:{WarehouseId}:{ArticleId}:{Type}" — kept entirely in C# (unlike
/// ArticleDiscount's SP-level guard) because the seasonal-wraparound decomposition logic
/// (DecomposeSeasonalRanges) only exists in C#; duplicating it in T-SQL would be a correctness/
/// drift risk. This test uses the simpler EVENT override type (no wraparound decomposition
/// involved) since the race being tested is about the DB-level guard, not the overlap-detection
/// logic itself.
///
/// Deliberately does NOT inherit TransactionalTestBase — same reasoning as
/// OrderApprovalRaceConditionTests/ArticleDiscountRaceConditionTests: an ambient TransactionScope
/// would enlist both "concurrent" calls into one shared transaction, which can never actually race
/// for the same app lock the way two independent real API requests do. Uses two independent DI
/// scopes/connections instead, and cleans up its own rows manually since there is no rollback
/// safety net here.
/// </summary>
public class ParLevelOverrideRaceConditionTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task ConcurrentCreateOverride_ForTheSameScopeAndOverlappingDates_OnlyOneWinsAndTheLoserGetsACleanConflict()
    {
        var namePrefix = $"PARLVLRACE{Guid.NewGuid():N}"[..20];
        Guid organizationToken;
        int organizationId;
        Guid warehouseToken, supplierToken, articleToken, familyToken;

        using (var setupScope = fixture.Services.CreateScope())
        {
            var setupData = new TestDataBuilder(setupScope.ServiceProvider);
            var setupContext = (TestRequestContext)setupScope.ServiceProvider.GetRequiredService<IRequestContext>();

            (organizationToken, organizationId) = await setupData.GetAssociateOrganizationAsync();
            var esJurisdiction = await setupData.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
            familyToken = await setupData.CreateFamilyAsync(namePrefix);

            setupContext.RoleLevel = 80;
            setupContext.OrganizationId = organizationId;
            setupContext.OrganizationTypeCode = "ASSOCIATE";

            warehouseToken = await setupData.CreateWarehouseAsync(organizationToken, esJurisdiction, namePrefix);
            supplierToken = await setupData.CreateSupplierAsync(organizationToken, namePrefix);
            articleToken = await setupData.CreateArticleAsync(supplierToken, familyToken, namePrefix);

            var mediator = setupScope.ServiceProvider.GetRequiredService<IMediator>();
            var baseLevel = await mediator.Send(new CreateParLevelCommandRequest
            {
                WarehouseToken = warehouseToken,
                ArticleToken = articleToken,
                MinimumQuantity = 5,
                ReorderQuantity = 10
            });
            baseLevel.Success.Should().BeTrue(string.Join("; ", baseLevel.Errors.Select(e => $"{e.Code}: {e.Description}")));
        }

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            async Task<ApiResponse<CreateParLevelOverrideCommandResponse>> CreateOverrideAsync()
            {
                using var scope = fixture.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var context = (TestRequestContext)scope.ServiceProvider.GetRequiredService<IRequestContext>();
                context.RoleLevel = 80;
                context.OrganizationId = organizationId;
                context.OrganizationTypeCode = "ASSOCIATE";

                return await mediator.Send(new CreateParLevelOverrideCommandRequest
                {
                    WarehouseToken = warehouseToken,
                    ArticleToken = articleToken,
                    Type = ParLevelOverrideTypeCodes.Event,
                    MinimumQuantity = 20,
                    ReorderQuantity = 30,
                    StartDate = today,
                    EndDate = today.AddDays(10)
                });
            }

            var task1 = CreateOverrideAsync();
            var task2 = CreateOverrideAsync();
            var results = await Task.WhenAll(task1, task2);

            results.Count(r => r.Success).Should().Be(1, "exactly one of two genuinely-concurrent overlapping-EVENT-override creates for the same scope should win");

            var loser = results.Single(r => !r.Success);
            loser.StatusCode.Should().Be(400, "the loser must get the same clean, anticipated conflict the non-race pre-check already returns — never a raw 500");
            loser.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.ParLevelOverrideOverlap);

            // The real integrity check: exactly one override must exist for this (Warehouse,
            // Article) — never two, which is the state the race used to be able to produce despite
            // the C#-side pre-check.
            using var verifyScope = fixture.Services.CreateScope();
            var connectionFactory = verifyScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            await using var connection = connectionFactory.CreateConnection();
            var overrideCount = await connection.QuerySingleAsync<int>(
                """
                SELECT COUNT(*) FROM ParLevelOverrides o
                JOIN Warehouses w ON w.WarehouseId = o.WarehouseId
                JOIN Articles a ON a.ArticleId = o.ArticleId
                WHERE w.WarehouseToken = @warehouseToken AND a.ArticleToken = @articleToken
                """,
                new { warehouseToken, articleToken });
            overrideCount.Should().Be(1);
        }
        finally
        {
            await CleanupAsync(warehouseToken, supplierToken, articleToken, familyToken);
        }
    }

    private async Task CleanupAsync(Guid warehouseToken, Guid supplierToken, Guid articleToken, Guid familyToken)
    {
        using var scope = fixture.Services.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            """
            DECLARE @WarehouseId INT = (SELECT WarehouseId FROM Warehouses WHERE WarehouseToken = @warehouseToken);
            DECLARE @ArticleId INT = (SELECT ArticleId FROM Articles WHERE ArticleToken = @articleToken);
            DECLARE @SupplierId INT = (SELECT SupplierId FROM Suppliers WHERE SupplierToken = @supplierToken);
            DECLARE @FamilyId INT = (SELECT FamilyId FROM Families WHERE FamilyToken = @familyToken);

            DELETE FROM ParLevelOverrides WHERE WarehouseId = @WarehouseId AND ArticleId = @ArticleId;
            DELETE FROM ParLevels WHERE WarehouseId = @WarehouseId AND ArticleId = @ArticleId;
            DELETE FROM ArticlePackagingLevels WHERE ArticleId = @ArticleId;
            DELETE FROM Articles WHERE ArticleId = @ArticleId;
            DELETE FROM OrganizationSuppliers WHERE SupplierId = @SupplierId;
            DELETE FROM Notifications WHERE UserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId);
            DELETE FROM AuditLogs WHERE ActorUserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId) OR EffectiveUserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId);
            DELETE FROM Users WHERE SupplierId = @SupplierId;
            DELETE FROM Suppliers WHERE SupplierId = @SupplierId;
            DELETE FROM Warehouses WHERE WarehouseId = @WarehouseId;
            DELETE FROM Families WHERE FamilyId = @FamilyId;
            """,
            new { warehouseToken, articleToken, supplierToken, familyToken });
    }
}
