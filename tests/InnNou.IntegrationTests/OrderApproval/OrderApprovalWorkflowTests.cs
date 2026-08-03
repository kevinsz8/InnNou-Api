using FluentAssertions;
using InnNou.Application.Common;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.OrderApproval;

/// <summary>
/// Regression coverage for OrderService's Submit/Approve/Reject state machine: crossing a
/// Family's spend threshold on Submit blocks PurchaseOrder creation behind one PENDING
/// OrderApprovalStep per triggered Level; levels are sequential (a higher Level can't be decided
/// before every lower one for the same Order+Family is APPROVED); full approval auto-completes
/// the submission with no second Submit click; a rejection reverts the Order to DRAFT and cancels
/// every other still-PENDING sibling step. Exercised through the real Order -> Submit -> Approve/
/// Reject pipeline, one article/one Family per Order throughout.
/// </summary>
public class OrderApprovalWorkflowTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    private const decimal UnitPrice = 10.00m;

    private async Task<(Guid OrganizationToken, int OrganizationId, Guid WarehouseToken, Guid ArticleToken, Guid FamilyToken)> SetupCatalogAsync(string namePrefix)
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync($"{namePrefix}_FAM");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, $"{namePrefix} Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, $"{namePrefix} Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, $"{namePrefix} Article");
        await Data.CreateArticlePriceAsync(articleToken, price: UnitPrice);

        return (organizationToken, organizationId, warehouseToken, articleToken, familyToken);
    }

    /// <summary>Restores the Admin/org context every helper call above needs, after a test has
    /// temporarily switched <see cref="TransactionalTestBase.Context"/> to act as a specific
    /// approver (who typically has no OrganizationTypeCode/RoleLevel matching that shape).</summary>
    private void ActAsAdmin(int organizationId)
    {
        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";
    }

    private void ActAsUser(Guid userToken)
    {
        Context.RoleLevel = 1;
        Context.ActorUserToken = userToken;
        Context.EffectiveUserToken = userToken;
        Context.OrganizationId = null;
        Context.OrganizationTypeCode = null;
    }

    [Fact]
    public async Task Submit_CrossingAThreshold_CreatesAPendingStepAndBlocksPurchaseOrderCreation()
    {
        var (organizationToken, organizationId, warehouseToken, articleToken, familyToken) = await SetupCatalogAsync("APPTEST_TRIGGER");
        var approverToken = await Data.CreateApproverUserAsync(organizationId, "APPTEST_TRIGGER_APPROVER");
        ActAsAdmin(organizationId);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 1, thresholdAmount: 15m, approverUserToken: approverToken);

        // 2 x 10.00 = 20.00 >= the 15.00 Level-1 threshold.
        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);

        var order = await Data.GetOrderAsync(orderToken);
        order.Status.Should().Be(OrderStatusCodes.PendingApproval);
        order.ApprovalSteps.Should().ContainSingle(s => s.Level == 1 && s.Status == OrderApprovalStepStatusCodes.Pending);
    }

    [Fact]
    public async Task ApprovingTheOnlyRequiredStep_AutoCompletesTheSubmission()
    {
        var (organizationToken, organizationId, warehouseToken, articleToken, familyToken) = await SetupCatalogAsync("APPTEST_AUTO");
        var approverToken = await Data.CreateApproverUserAsync(organizationId, "APPTEST_AUTO_APPROVER");
        ActAsAdmin(organizationId);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 1, thresholdAmount: 15m, approverUserToken: approverToken);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var order = await Data.GetOrderAsync(orderToken);
        var step = order.ApprovalSteps.Single();

        ActAsUser(approverToken);
        await Data.ApproveOrderApprovalStepAsync(step.OrderApprovalStepToken);

        ActAsAdmin(organizationId);
        var completedOrder = await Data.GetOrderAsync(orderToken);
        completedOrder.Status.Should().Be(OrderStatusCodes.Submitted, "the only required approval was granted - Submit should complete with no second click");
    }

    [Fact]
    public async Task Approving_ALaterLevel_BeforeAnEarlierOne_IsRejectedWithPriorLevelPending()
    {
        var (organizationToken, organizationId, warehouseToken, articleToken, familyToken) = await SetupCatalogAsync("APPTEST_ORDER");
        var level1Approver = await Data.CreateApproverUserAsync(organizationId, "APPTEST_ORDER_L1");
        var level2Approver = await Data.CreateApproverUserAsync(organizationId, "APPTEST_ORDER_L2");
        ActAsAdmin(organizationId);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 1, thresholdAmount: 10m, approverUserToken: level1Approver);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 2, thresholdAmount: 15m, approverUserToken: level2Approver);

        // 20.00 crosses both levels - both steps are created PENDING at Submit time.
        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var order = await Data.GetOrderAsync(orderToken);
        var level2Step = order.ApprovalSteps.Single(s => s.Level == 2);

        ActAsUser(level2Approver);
        var act = () => Data.ApproveOrderApprovalStepAsync(level2Step.OrderApprovalStepToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ORDER_APPROVAL_STEP_PRIOR_LEVEL_PENDING*");
    }

    [Fact]
    public async Task ApprovingEveryLevelInOrder_AutoCompletesTheSubmission()
    {
        var (organizationToken, organizationId, warehouseToken, articleToken, familyToken) = await SetupCatalogAsync("APPTEST_MULTI");
        var level1Approver = await Data.CreateApproverUserAsync(organizationId, "APPTEST_MULTI_L1");
        var level2Approver = await Data.CreateApproverUserAsync(organizationId, "APPTEST_MULTI_L2");
        ActAsAdmin(organizationId);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 1, thresholdAmount: 10m, approverUserToken: level1Approver);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 2, thresholdAmount: 15m, approverUserToken: level2Approver);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var order = await Data.GetOrderAsync(orderToken);
        var level1Step = order.ApprovalSteps.Single(s => s.Level == 1);
        var level2Step = order.ApprovalSteps.Single(s => s.Level == 2);

        ActAsUser(level1Approver);
        await Data.ApproveOrderApprovalStepAsync(level1Step.OrderApprovalStepToken);

        ActAsAdmin(organizationId);
        var afterLevel1 = await Data.GetOrderAsync(orderToken);
        afterLevel1.Status.Should().Be(OrderStatusCodes.PendingApproval, "Level 2 has not been decided yet");

        ActAsUser(level2Approver);
        await Data.ApproveOrderApprovalStepAsync(level2Step.OrderApprovalStepToken);

        ActAsAdmin(organizationId);
        var completedOrder = await Data.GetOrderAsync(orderToken);
        completedOrder.Status.Should().Be(OrderStatusCodes.Submitted, "both required levels are now approved");
    }

    [Fact]
    public async Task RejectingAStep_RevertsTheOrderToDraftAndCancelsPendingSiblings()
    {
        var (organizationToken, organizationId, warehouseToken, articleToken, familyToken) = await SetupCatalogAsync("APPTEST_REJECT");
        var level1Approver = await Data.CreateApproverUserAsync(organizationId, "APPTEST_REJECT_L1");
        var level2Approver = await Data.CreateApproverUserAsync(organizationId, "APPTEST_REJECT_L2");
        ActAsAdmin(organizationId);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 1, thresholdAmount: 10m, approverUserToken: level1Approver);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 2, thresholdAmount: 15m, approverUserToken: level2Approver);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var order = await Data.GetOrderAsync(orderToken);
        var level1Step = order.ApprovalSteps.Single(s => s.Level == 1);

        ActAsUser(level1Approver);
        await Data.RejectOrderApprovalStepAsync(level1Step.OrderApprovalStepToken, "Price looks wrong, please review before resubmitting.");

        ActAsAdmin(organizationId);
        var revertedOrder = await Data.GetOrderAsync(orderToken);
        revertedOrder.Status.Should().Be(OrderStatusCodes.Draft);
        revertedOrder.ApprovalSteps.Should().ContainSingle(s => s.Level == 1 && s.Status == OrderApprovalStepStatusCodes.Rejected);
        revertedOrder.ApprovalSteps.Should().ContainSingle(s => s.Level == 2 && s.Status == OrderApprovalStepStatusCodes.Cancelled,
            "every other still-pending sibling step for this Order must be cancelled alongside the rejection");
    }

    [Fact]
    public async Task Approving_AsSomeoneOtherThanTheDesignatedApprover_IsForbidden()
    {
        var (organizationToken, organizationId, warehouseToken, articleToken, familyToken) = await SetupCatalogAsync("APPTEST_FORBIDDEN");
        var designatedApprover = await Data.CreateApproverUserAsync(organizationId, "APPTEST_FORBIDDEN_REAL");
        var someoneElse = await Data.CreateApproverUserAsync(organizationId, "APPTEST_FORBIDDEN_OTHER");
        ActAsAdmin(organizationId);
        await Data.CreateFamilyApprovalThresholdAsync(organizationToken, familyToken, level: 1, thresholdAmount: 15m, approverUserToken: designatedApprover);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var order = await Data.GetOrderAsync(orderToken);
        var step = order.ApprovalSteps.Single();

        ActAsUser(someoneElse);
        var act = () => Data.ApproveOrderApprovalStepAsync(step.OrderApprovalStepToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ORDER_APPROVAL_STEP_FORBIDDEN*");
    }
}
