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

namespace InnNou.IntegrationTests.OrderApproval;

/// <summary>
/// Regression coverage for the concurrent-decide race fixed 2026-08-07: two genuinely-simultaneous
/// ApproveOrderApprovalStepCommandRequest calls on the same last-pending step used to both pass
/// sp_OrderApprovalStep_Approve's own "IF NOT EXISTS ... PENDING" pre-check before either UPDATE
/// landed (the UPDATE's WHERE clause never re-checked status despite a comment claiming it did),
/// so both proceeded into OrderService.CompleteSubmissionAsync and the loser's PurchaseOrder
/// INSERT hit UX_PurchaseOrder_Order_Supplier's unique index as a raw, unhandled SqlException
/// (500 UNHANDLED_ERROR) instead of the anticipated 409 ORDER_APPROVAL_STEP_ALREADY_DECIDED.
/// Fixed by making the step UPDATE itself the atomic guard (WHERE ... AND StatusId = Pending,
/// checking @@ROWCOUNT) in both sp_OrderApprovalStep_Approve and _Reject, backed by a
/// defense-in-depth SqlException(2601/2627) catch in CompleteSubmissionAsync for the separate,
/// narrower race between this path and SubmitAsync's own documented self-healing retry.
///
/// Deliberately does NOT inherit TransactionalTestBase. Its ambient TransactionScope enlists
/// every connection opened during a test into ONE shared System.Transactions.Transaction, so two
/// "concurrent" calls under it don't actually compete for the same row lock the way two
/// independent real API requests would — enlisting a second connection concurrently would either
/// require MSDTC promotion or simply serialize, neither of which exercises the real race. This
/// test instead opens two fully independent DI scopes/connections with no ambient transaction —
/// exactly like two real concurrent HTTP requests — and cleans up its own rows manually in a
/// finally block, since there is no TransactionScope rollback safety net here.
/// </summary>
public class OrderApprovalRaceConditionTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task ConcurrentApprove_OnTheLastPendingStep_OnlyOneWinsAndTheLoserGetsACleanConflict()
    {
        var namePrefix = $"RACE{Guid.NewGuid():N}"[..16];
        Guid organizationToken;
        int organizationId;
        Guid warehouseToken, articleToken, supplierToken, familyToken, approverToken, orderToken;

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
            await setupData.CreateArticlePriceAsync(articleToken, price: 20.00m);
            approverToken = await setupData.CreateApproverUserAsync(organizationId, namePrefix);
            await setupData.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 1, thresholdAmount: 10m, approverUserToken: approverToken);

            orderToken = await setupData.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 1);
        }

        try
        {
            Guid stepToken;
            using (var readScope = fixture.Services.CreateScope())
            {
                var readData = new TestDataBuilder(readScope.ServiceProvider);
                var order = await readData.GetOrderAsync(orderToken);
                stepToken = order.ApprovalSteps.Single().OrderApprovalStepToken;
            }

            async Task<ApiResponse<ApproveOrderApprovalStepCommandResponse>> DecideAsync()
            {
                using var scope = fixture.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var context = (TestRequestContext)scope.ServiceProvider.GetRequiredService<IRequestContext>();
                context.RoleLevel = 1;
                context.ActorUserToken = approverToken;
                context.EffectiveUserToken = approverToken;
                context.OrganizationId = null;
                context.OrganizationTypeCode = null;

                return await mediator.Send(new ApproveOrderApprovalStepCommandRequest { OrderApprovalStepToken = stepToken });
            }

            var task1 = DecideAsync();
            var task2 = DecideAsync();
            var results = await Task.WhenAll(task1, task2);

            results.Count(r => r.Success).Should().Be(1, "exactly one of two genuinely-concurrent decisions on the same step should win");

            var loser = results.Single(r => !r.Success);
            loser.StatusCode.Should().Be(409, "the loser must get a clean, anticipated conflict — never a raw 500");
            loser.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.OrderApprovalStepAlreadyDecided);

            // The real integrity check: exactly one PurchaseOrder must exist for this Order —
            // never zero (both losing) and never two (the original raw-500 bug's failure mode,
            // where the DB unique index was the only thing stopping an actual duplicate).
            using var verifyScope = fixture.Services.CreateScope();
            var connectionFactory = verifyScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            await using var connection = connectionFactory.CreateConnection();
            var purchaseOrderCount = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM PurchaseOrder po JOIN [Order] o ON o.OrderId = po.OrderId WHERE o.OrderToken = @orderToken",
                new { orderToken });
            purchaseOrderCount.Should().Be(1);
        }
        finally
        {
            await CleanupAsync(orderToken, familyToken, articleToken, supplierToken, warehouseToken, approverToken);
        }
    }

    /// <summary>No TransactionScope rollback net here (see class doc) — every row this test
    /// created is a real, committed row in InnNou_Test that must be deleted explicitly, in
    /// dependency order (every FK in this schema is NO_ACTION, so cascade delete isn't an
    /// option). Targeted entirely by the tokens/IDs this test itself generated, so it can never
    /// touch another test's data even though it runs outside the usual isolation mechanism.</summary>
    private async Task CleanupAsync(Guid orderToken, Guid familyToken, Guid articleToken, Guid supplierToken, Guid warehouseToken, Guid approverToken)
    {
        using var scope = fixture.Services.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            """
            DECLARE @OrderId INT = (SELECT OrderId FROM [Order] WHERE OrderToken = @orderToken);
            DECLARE @ArticleId INT = (SELECT ArticleId FROM Articles WHERE ArticleToken = @articleToken);
            DECLARE @SupplierId INT = (SELECT SupplierId FROM Suppliers WHERE SupplierToken = @supplierToken);
            DECLARE @WarehouseId INT = (SELECT WarehouseId FROM Warehouses WHERE WarehouseToken = @warehouseToken);
            DECLARE @FamilyId INT = (SELECT FamilyId FROM Families WHERE FamilyToken = @familyToken);
            DECLARE @ApproverUserId INT = (SELECT UserId FROM Users WHERE UserToken = @approverToken);

            DELETE FROM PurchaseOrderLine WHERE PurchaseOrderId IN (SELECT PurchaseOrderId FROM PurchaseOrder WHERE OrderId = @OrderId);
            DELETE FROM PurchaseOrder WHERE OrderId = @OrderId;
            DELETE FROM OrderApprovalSteps WHERE OrderId = @OrderId;
            DELETE FROM OrderLine WHERE OrderId = @OrderId;
            DELETE FROM [Order] WHERE OrderId = @OrderId;
            DELETE FROM FamilyApprovalThresholds WHERE FamilyId = @FamilyId;
            DELETE FROM ArticlePrices WHERE ArticleId = @ArticleId;
            DELETE FROM ArticlePackagingLevels WHERE ArticleId = @ArticleId;
            DELETE FROM Articles WHERE ArticleId = @ArticleId;
            DELETE FROM OrganizationSuppliers WHERE SupplierId = @SupplierId;
            DELETE FROM Notifications WHERE UserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId);
            DELETE FROM AuditLogs WHERE ActorUserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId) OR EffectiveUserId IN (SELECT UserId FROM Users WHERE SupplierId = @SupplierId);
            DELETE FROM Users WHERE SupplierId = @SupplierId;
            DELETE FROM Suppliers WHERE SupplierId = @SupplierId;
            DELETE FROM Warehouses WHERE WarehouseId = @WarehouseId;
            DELETE FROM Families WHERE FamilyId = @FamilyId;
            DELETE FROM Notifications WHERE UserId = @ApproverUserId;
            DELETE FROM AuditLogs WHERE ActorUserId = @ApproverUserId OR EffectiveUserId = @ApproverUserId;
            DELETE FROM Users WHERE UserId = @ApproverUserId;
            """,
            new { orderToken, articleToken, supplierToken, warehouseToken, familyToken, approverToken });
    }
}
