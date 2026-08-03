using FluentAssertions;
using InnNou.Application.Common;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.SupplierInvoices;

/// <summary>
/// Regression coverage for SupplierInvoiceService.CreateAsync's 3-way matching: the buyer-typed
/// VAT breakdown (Base Fra per tax rate) is compared, net-only, against what the selected
/// GoodsReceipts' own frozen tax numbers add up to - within tolerance -> MATCHED, outside ->
/// DISCREPANCY (still saved, never blocked). Also covers the two hard invariants around which
/// receipts an invoice may consolidate: the per-organization single-PO-per-invoice policy, and
/// "a receipt belongs to at most one invoice, ever" (DB-enforced uniqueness). Exercised through
/// the real Order -> PurchaseOrder -> GoodsReceipt -> SupplierInvoice pipeline.
/// </summary>
public class SupplierInvoiceMatchingTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    private const decimal UnitPrice = 10.00m;

    private async Task<(Guid OrganizationToken, int OrganizationId, Guid WarehouseToken, Guid SupplierToken, Guid ArticleToken)> SetupCatalogAsync(string namePrefix)
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

        return (organizationToken, organizationId, warehouseToken, supplierToken, articleToken);
    }

    [Fact]
    public async Task CreateAsync_MarksInvoiceMatched_WhenBaseFraEqualsTheReceiptsExpectedNetTotal()
    {
        var (organizationToken, _, warehouseToken, supplierToken, articleToken) = await SetupCatalogAsync("SITEST_MATCH");

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);
        var receipt = await Data.ReceiveFullyAsync(purchaseOrder.PurchaseOrderToken, quantity: 2, deliveryNoteNumber: "ALB-SI-MATCH");

        // 2 x 10.00 = 20.00 net - no tolerance configured anywhere in this org's ancestry, so an
        // exact Base Fra match is required (default resolves to 0%/0.00, not a hard block).
        var invoice = await Data.CreateSupplierInvoiceAsync(
            organizationToken, supplierToken, "FACT-MATCH-001", [receipt], UnitPrice, baseAmountOverride: 20.00m);

        invoice.Status.Should().Be(SupplierInvoiceStatusCodes.Matched);
    }

    [Fact]
    public async Task CreateAsync_MarksInvoiceDiscrepancy_WhenBaseFraDiffersUnderAZeroTolerance()
    {
        var (organizationToken, _, warehouseToken, supplierToken, articleToken) = await SetupCatalogAsync("SITEST_NOTOL");

        // Explicit 0%/0.00, rather than relying on "nothing configured anywhere in the ancestry"
        // (which also resolves to an in-memory 0/0 default, but InnNou_Test is a snapshot of real
        // dev data and this shared seeded ASSOCIATE org may already carry a real tolerance row
        // from manual UI testing - pinning it here keeps the test deterministic either way).
        await Data.UpsertSupplierInvoiceToleranceAsync(organizationToken, tolerancePercent: 0m, toleranceAmount: 0m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);
        var receipt = await Data.ReceiveFullyAsync(purchaseOrder.PurchaseOrderToken, quantity: 2, deliveryNoteNumber: "ALB-SI-NOTOL");

        // Expected net is 20.00; typing 20.01 (1 cent off) under a 0%/0.00 tolerance must flag a
        // discrepancy - but the invoice still saves, per this codebase's "out-of-tolerance does
        // not block saving" design (a flag for manual review, not an approval gate).
        var invoice = await Data.CreateSupplierInvoiceAsync(
            organizationToken, supplierToken, "FACT-NOTOL-001", [receipt], UnitPrice, baseAmountOverride: 20.01m);

        invoice.Status.Should().Be(SupplierInvoiceStatusCodes.Discrepancy);
    }

    [Fact]
    public async Task CreateAsync_MarksInvoiceMatched_WhenBaseFraDiffersButStaysWithinConfiguredTolerance()
    {
        var (organizationToken, _, warehouseToken, supplierToken, articleToken) = await SetupCatalogAsync("SITEST_TOL");

        await Data.UpsertSupplierInvoiceToleranceAsync(organizationToken, tolerancePercent: 5m, toleranceAmount: 1.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);
        var receipt = await Data.ReceiveFullyAsync(purchaseOrder.PurchaseOrderToken, quantity: 2, deliveryNoteNumber: "ALB-SI-TOL");

        // Expected net 20.00, typed 20.50 - a 0.50 diff is within both the 5% (1.00) and the
        // 1.00 fixed-amount tolerance, so it must still resolve MATCHED despite not being exact.
        var invoice = await Data.CreateSupplierInvoiceAsync(
            organizationToken, supplierToken, "FACT-TOL-001", [receipt], UnitPrice, baseAmountOverride: 20.50m);

        invoice.Status.Should().Be(SupplierInvoiceStatusCodes.Matched);
    }

    [Fact]
    public async Task CreateAsync_RejectsConsolidatingMultiplePurchaseOrders_WhenThePolicyDisallowsIt()
    {
        var (organizationToken, _, warehouseToken, supplierToken, articleToken) = await SetupCatalogAsync("SITEST_POLICY");

        await Data.UpsertSupplierInvoicePurchaseOrderPolicyAsync(organizationToken, allowMultiplePurchaseOrders: false);

        // Two separate Orders against the same supplier become two separate PurchaseOrders.
        var firstOrderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 1);
        var firstPurchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(firstOrderToken);
        var firstReceipt = await Data.ReceiveFullyAsync(firstPurchaseOrder.PurchaseOrderToken, quantity: 1, deliveryNoteNumber: "ALB-SI-POLICY-1");

        var secondOrderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 1);
        var secondPurchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(secondOrderToken);
        var secondReceipt = await Data.ReceiveFullyAsync(secondPurchaseOrder.PurchaseOrderToken, quantity: 1, deliveryNoteNumber: "ALB-SI-POLICY-2");

        var act = () => Data.CreateSupplierInvoiceAsync(
            organizationToken, supplierToken, "FACT-POLICY-001", [firstReceipt, secondReceipt], UnitPrice);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SUPPLIER_INVOICE_MULTIPLE_PURCHASE_ORDERS_NOT_ALLOWED*",
                "this organization's policy restricts an invoice to a single purchase order, and these two receipts belong to different POs");
    }

    [Fact]
    public async Task CreateAsync_RejectsAReceiptThatWasAlreadyInvoiced()
    {
        var (organizationToken, _, warehouseToken, supplierToken, articleToken) = await SetupCatalogAsync("SITEST_DUPE");

        // Only 2 of the 4 ordered units are received, so the PurchaseOrder stays
        // PARTIALLY_RECEIVED (not RECEIVED) even after this one receipt gets invoiced - keeping
        // it eligible for a second CreateAsync call, so that call actually reaches the
        // per-receipt already-invoiced check instead of failing earlier on PO status. (A fully
        // RECEIVED PO whose only receipt gets invoiced immediately flips to INVOICED, and a
        // second attempt against it would hit SUPPLIER_INVOICE_PURCHASE_ORDER_NOT_RECEIVED
        // first - a different, earlier gate, not the one this test targets.)
        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 4);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);
        var receipt = await Data.ReceiveSingleLineAsync(purchaseOrder.PurchaseOrderToken, "ALB-SI-DUPE", quantityAccepted: 2);

        await Data.CreateSupplierInvoiceAsync(organizationToken, supplierToken, "FACT-DUPE-001", [receipt], UnitPrice, baseAmountOverride: 20.00m);

        // A GoodsReceipt belongs to at most one invoice, ever - DB-enforced via a UNIQUE
        // constraint on SupplierInvoiceGoodsReceipts.GoodsReceiptId.
        var act = () => Data.CreateSupplierInvoiceAsync(
            organizationToken, supplierToken, "FACT-DUPE-002", [receipt], UnitPrice, baseAmountOverride: 20.00m);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SUPPLIER_INVOICE_GOODS_RECEIPT_ALREADY_INVOICED*");
    }
}
