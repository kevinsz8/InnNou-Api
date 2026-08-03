using FluentAssertions;
using InnNou.Application.Requests;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.Tax;

/// <summary>
/// Regression coverage for the resolution chain
/// COALESCE(Article.TaxCategoryId, FamilyTaxCategoryOverrides for the receiving Warehouse's
/// jurisdiction, Family.DefaultTaxCategoryId) - added 2026-08-05 to let a Family (e.g. Bebidas)
/// resolve to a different tax category, not just a different rate, per country. Exercised through
/// the real Order -> PurchaseOrder -> GoodsReceipt pipeline (not the SP in isolation), because
/// that's where this actually gets computed and frozen.
/// </summary>
public class FamilyTaxCategoryOverrideTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    [Fact]
    public async Task GoodsReceipt_UsesFamilyDefaultCategory_WhenNoOverrideExistsForTheWarehousesJurisdiction()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esMainlandJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");

        // A fresh Family, defaulted to GENERAL, with no override anywhere - isolates this test
        // from any override left behind by another test or a manual session.
        var familyToken = await Data.CreateFamilyAsync("TAXTEST_DEFAULT");

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esMainlandJurisdiction, "TaxTest ES Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "TaxTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "TaxTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);
        var receipt = await Data.ReceiveFullyAsync(purchaseOrder.PurchaseOrderToken, quantity: 2, deliveryNoteNumber: "ALB-TEST-DEFAULT");

        var line = receipt.Lines.Single();
        line.TaxCategoryCode.Should().Be("GENERAL", "the family has no override for ES_MAINLAND_BALEARIC, so it must fall back to its own default");
        line.TaxRatePercent.Should().Be(21.00000000m);
        line.TaxableAmount.Should().Be(20.00000000m); // 2 x 10.00
        line.TaxAmount.Should().Be(4.20000000m);       // 20.00 x 21%
    }

    [Fact]
    public async Task GoodsReceipt_UsesTheOverrideCategory_WhenOneExistsForTheWarehousesJurisdiction()
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var costaRicaJurisdiction = await Data.GetTaxJurisdictionTokenAsync("CR_STANDARD");
        var reducedCategory = await Data.GetTaxCategoryTokenAsync("REDUCED");

        var familyToken = await Data.CreateFamilyAsync("TAXTEST_OVERRIDE");

        // FamilyTaxCategoryOverrides is SuperAdmin-only config (same reasoning as TaxRate/
        // TaxJurisdiction: a jurisdiction's tax facts are legal, not an org-level business
        // setting) - switch context up before this one call, then back down for everything else.
        Context.RoleLevel = 100;
        await Mediator.Send(new UpsertFamilyTaxCategoryOverrideCommandRequest
        {
            FamilyToken = familyToken,
            TaxJurisdictionToken = costaRicaJurisdiction,
            TaxCategoryToken = reducedCategory
        });

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, costaRicaJurisdiction, "TaxTest CR Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "TaxTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "TaxTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 2);
        var purchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);
        var receipt = await Data.ReceiveFullyAsync(purchaseOrder.PurchaseOrderToken, quantity: 2, deliveryNoteNumber: "ALB-TEST-OVERRIDE");

        var line = receipt.Lines.Single();
        line.TaxCategoryCode.Should().Be("REDUCED", "this family has an override for CR_STANDARD - it must win over its own GENERAL default");
        line.TaxRatePercent.Should().Be(4.00000000m); // Costa Rica's REDUCED tier, not GENERAL's 13%
        line.TaxableAmount.Should().Be(20.00000000m);
        line.TaxAmount.Should().Be(0.80000000m);       // 20.00 x 4%
    }

    [Fact]
    public async Task SameFamily_ResolvesDifferentCategories_InDifferentJurisdictions_WithinTheSameOverrideSet()
    {
        // The core claim of this whole feature: the SAME Family, with ONE override configured
        // for ONE specific jurisdiction, must resolve differently depending on which warehouse
        // receives the goods - not globally, and not per-Article.
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esMainlandJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var andorraJurisdiction = await Data.GetTaxJurisdictionTokenAsync("AD_STANDARD");
        var superReducedCategory = await Data.GetTaxCategoryTokenAsync("SUPER_REDUCED");

        var familyToken = await Data.CreateFamilyAsync("TAXTEST_MULTI");

        Context.RoleLevel = 100;
        await Mediator.Send(new UpsertFamilyTaxCategoryOverrideCommandRequest
        {
            FamilyToken = familyToken,
            TaxJurisdictionToken = andorraJurisdiction,
            TaxCategoryToken = superReducedCategory
        });

        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var supplierToken = await Data.CreateSupplierAsync(organizationToken, "TaxTest Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, "TaxTest Article");
        await Data.CreateArticlePriceAsync(articleToken, price: 10.00m);

        var esWarehouse = await Data.CreateWarehouseAsync(organizationToken, esMainlandJurisdiction, "TaxTest ES Warehouse");
        var esOrder = await Data.CreateSubmittedOrderAsync(esWarehouse, articleToken, quantity: 1);
        var esPurchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(esOrder);
        var esReceipt = await Data.ReceiveFullyAsync(esPurchaseOrder.PurchaseOrderToken, quantity: 1, deliveryNoteNumber: "ALB-TEST-ES");

        var adWarehouse = await Data.CreateWarehouseAsync(organizationToken, andorraJurisdiction, "TaxTest AD Warehouse");
        var adOrder = await Data.CreateSubmittedOrderAsync(adWarehouse, articleToken, quantity: 1);
        var adPurchaseOrder = await Data.GetSinglePurchaseOrderForOrderAsync(adOrder);
        var adReceipt = await Data.ReceiveFullyAsync(adPurchaseOrder.PurchaseOrderToken, quantity: 1, deliveryNoteNumber: "ALB-TEST-AD");

        esReceipt.Lines.Single().TaxCategoryCode.Should().Be("GENERAL", "Spain has no override for this family - falls back to the default");
        adReceipt.Lines.Single().TaxCategoryCode.Should().Be("SUPER_REDUCED", "Andorra has an explicit override for this family");
    }
}
