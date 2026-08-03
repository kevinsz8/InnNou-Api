using FluentAssertions;
using InnNou.Application.Common;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.GoodsReceipts;

/// <summary>
/// Regression coverage for PurchaseOrderService.CreateGoodsReceiptAsync's status recompute logic:
/// cumulative Accepted quantity per line (across every receipt so far), compared against each
/// line's ordered Quantity - every line fully covered -> RECEIVED; any line partially covered ->
/// PARTIALLY_RECEIVED; nothing accepted anywhere (e.g. an all-Rejected receipt) -> stays SENT.
/// Exercised through the real Order -> PurchaseOrder -> GoodsReceipt pipeline, one article/one
/// line per PurchaseOrder throughout (this builder's simplest shape), since the status transition
/// itself - not multi-line aggregation - is what's under test here.
/// </summary>
public class GoodsReceiptStatusTransitionTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    [Fact]
    public async Task GoodsReceipt_FullAcceptance_MarksPurchaseOrderReceived()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync("GRTEST_FULL");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, "GRTest Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "GRTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "GRTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 3);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);

        await Data.ReceiveFullyAsync(purchaseOrder.PurchaseOrderToken, quantity: 3, deliveryNoteNumber: "ALB-FULL");

        var status = await Data.GetPurchaseOrderStatusAsync(purchaseOrder.PurchaseOrderToken);
        status.Should().Be(PurchaseOrderStatusCodes.Received, "every unit ordered was accepted in a single receipt");
    }

    [Fact]
    public async Task GoodsReceipt_PartialAcceptance_LeavesPurchaseOrderPartiallyReceived()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync("GRTEST_PARTIAL");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, "GRTest Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "GRTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "GRTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 10);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);

        await Data.ReceiveSingleLineAsync(purchaseOrder.PurchaseOrderToken, "ALB-PARTIAL", quantityAccepted: 4);

        var status = await Data.GetPurchaseOrderStatusAsync(purchaseOrder.PurchaseOrderToken);
        status.Should().Be(PurchaseOrderStatusCodes.PartiallyReceived, "only 4 of the 10 ordered units have been accepted so far");
    }

    [Fact]
    public async Task GoodsReceipt_CompletingTheRemainder_MarksPurchaseOrderReceived()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync("GRTEST_REMAINDER");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, "GRTest Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "GRTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "GRTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 10);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);

        await Data.ReceiveSingleLineAsync(purchaseOrder.PurchaseOrderToken, "ALB-REMAINDER-1", quantityAccepted: 4);
        (await Data.GetPurchaseOrderStatusAsync(purchaseOrder.PurchaseOrderToken)).Should().Be(PurchaseOrderStatusCodes.PartiallyReceived);

        // A second, separate receipt (its own delivery note) completes the remaining 6 units -
        // the status recompute must look at the cumulative Accepted across BOTH receipts, not
        // just this one.
        await Data.ReceiveSingleLineAsync(purchaseOrder.PurchaseOrderToken, "ALB-REMAINDER-2", quantityAccepted: 6);

        var status = await Data.GetPurchaseOrderStatusAsync(purchaseOrder.PurchaseOrderToken);
        status.Should().Be(PurchaseOrderStatusCodes.Received, "4 + 6 = 10, the full ordered quantity, across two separate receipts");
    }

    [Fact]
    public async Task GoodsReceipt_AllRejected_LeavesPurchaseOrderAtSent()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync("GRTEST_REJECTED");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, "GRTest Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "GRTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "GRTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 5);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);

        await Data.ReceiveSingleLineAsync(
            purchaseOrder.PurchaseOrderToken, "ALB-ALLREJECTED",
            quantityRejected: 5, rejectionReason: "Damaged in transit");

        var status = await Data.GetPurchaseOrderStatusAsync(purchaseOrder.PurchaseOrderToken);
        status.Should().Be(PurchaseOrderStatusCodes.Sent, "nothing was Accepted - a wholly rejected delivery hasn't fulfilled any part of the order yet");
    }

    [Fact]
    public async Task GoodsReceipt_AcceptingMoreThanOrdered_IsRejected()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync("GRTEST_OVER");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, "GRTest Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "GRTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "GRTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);

        var act = () => Data.ReceiveSingleLineAsync(purchaseOrder.PurchaseOrderToken, "ALB-OVER", quantityAccepted: 3);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*GOODS_RECEIPT_OVER_RECEIPT_NOT_ALLOWED*",
                "no tolerance is allowed - any supplier surplus must be recorded as Courtesy or Rejected, never silently accepted");
    }

    [Fact]
    public async Task GoodsReceipt_AgainstAWarehouseNotConfiguredToReceive_IsRejected()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync("GRTEST_NORECEIVE");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, "GRTest Non-Receiving Warehouse", canReceivePurchases: false);
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "GRTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "GRTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        // CanReceivePurchases only gates goods receipt creation, not Order/PurchaseOrder creation
        // itself - the submission must succeed and only the receive attempt should fail.
        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 1);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);

        var act = () => Data.ReceiveSingleLineAsync(purchaseOrder.PurchaseOrderToken, "ALB-NORECEIVE", quantityAccepted: 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*GOODS_RECEIPT_WAREHOUSE_CANNOT_RECEIVE*");
    }
}
