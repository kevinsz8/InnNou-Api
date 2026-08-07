using FluentAssertions;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.IntegrationTests.TestFixtures;
using Xunit;

namespace InnNou.IntegrationTests.ArticleDiscounts;

/// <summary>
/// Regression coverage for the Article Discounts feature (see InnNou-Api CLAUDE.md's "Supplier
/// Discounts" section): resolution priority (Article > SubFamily > Family > supplier-wide, "most
/// specific wins, no stacking"), OrderService.AddLineAsync's live discount application and
/// BaseUnitPrice/DiscountTypeId/DiscountValue freeze onto OrderLine, the same freeze carrying
/// through to PurchaseOrderLine at Submit split time, and the scope/currency validation rules.
/// The Submit-time test also doubles as a regression guard for
/// BuildPurchaseOrderLineTable/PurchaseOrderLineTableType/sp_PurchaseOrderLine_CreateBatch's
/// column shape staying in sync (a genuine mismatch found and fixed while building this feature).
/// </summary>
public class ArticleDiscountTests(DatabaseFixture fixture) : TransactionalTestBase(fixture)
{
    private const decimal UnitPrice = 100.00m;

    private async Task<(Guid WarehouseToken, Guid SupplierToken, Guid ArticleToken, Guid FamilyToken)> SetupCatalogAsync(string namePrefix)
    {
        var (organizationToken, organizationId) = await Data.GetAssociateOrganizationAsync();
        var esJurisdiction = await Data.GetTaxJurisdictionTokenAsync("ES_MAINLAND_BALEARIC");
        var familyToken = await Data.CreateFamilyAsync($"{namePrefix}_FAM");

        // Same context shape OrderApprovalWorkflowTests.SetupCatalogAsync uses — CreateOrder/
        // AddLine/Submit's own hierarchy check needs a concrete OrganizationId+OrganizationTypeCode,
        // not just a bare RoleLevel-100 SuperAdmin context.
        Context.RoleLevel = 80;
        Context.OrganizationId = organizationId;
        Context.OrganizationTypeCode = "ASSOCIATE";

        var warehouseToken = await Data.CreateWarehouseAsync(organizationToken, esJurisdiction, $"{namePrefix} Warehouse");
        var supplierToken = await Data.CreateSupplierAsync(organizationToken, $"{namePrefix} Supplier");
        var articleToken = await Data.CreateArticleAsync(supplierToken, familyToken, $"{namePrefix} Article");
        await Data.CreateArticlePriceAsync(articleToken, price: UnitPrice);

        return (warehouseToken, supplierToken, articleToken, familyToken);
    }

    private async Task<InnNou.Application.Responses.Common.OrderLine> AddLineAndGetAsync(Guid warehouseToken, Guid articleToken)
    {
        var order = await Mediator.Send(new CreateOrderCommandRequest { WarehouseToken = warehouseToken });
        order.Success.Should().BeTrue(string.Join("; ", order.Errors.Select(e => $"{e.Code}: {e.Description}")));
        var orderToken = order.ReturnData!.Order!.OrderToken;

        var addLine = await Mediator.Send(new AddOrderLineCommandRequest { OrderToken = orderToken, ArticleToken = articleToken, Quantity = 1 });
        addLine.Success.Should().BeTrue(string.Join("; ", addLine.Errors.Select(e => $"{e.Code}: {e.Description}")));

        var fetched = await Data.GetOrderAsync(orderToken);
        return fetched.Lines.Single();
    }

    [Fact]
    public async Task AddLine_WithArticleAndFamilyDiscounts_PrefersTheArticleScopedOne()
    {
        var (warehouseToken, supplierToken, articleToken, familyToken) = await SetupCatalogAsync("ARTDISC_PRIORITY");
        var today = DateTime.UtcNow.Date;

        await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 10m, today, familyToken: familyToken);
        await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 20m, today, articleToken: articleToken);

        var line = await AddLineAndGetAsync(warehouseToken, articleToken);

        line.BaseUnitPrice.Should().Be(UnitPrice);
        line.DiscountTypeCode.Should().Be("PERCENTAGE");
        line.DiscountValue.Should().Be(20m);
        line.UnitPrice.Should().Be(UnitPrice * 0.8m);
    }

    [Fact]
    public async Task AddLine_WithOnlyFamilyDiscount_AppliesIt()
    {
        var (warehouseToken, supplierToken, articleToken, familyToken) = await SetupCatalogAsync("ARTDISC_FAMILY");
        await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 15m, DateTime.UtcNow.Date, familyToken: familyToken);

        var line = await AddLineAndGetAsync(warehouseToken, articleToken);

        line.DiscountTypeCode.Should().Be("PERCENTAGE");
        line.UnitPrice.Should().Be(UnitPrice * 0.85m);
    }

    [Fact]
    public async Task AddLine_WithOnlySupplierWideDiscount_AppliesIt()
    {
        var (warehouseToken, supplierToken, articleToken, _) = await SetupCatalogAsync("ARTDISC_SUPPLIER");
        await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 5m, DateTime.UtcNow.Date);

        var line = await AddLineAndGetAsync(warehouseToken, articleToken);

        line.DiscountTypeCode.Should().Be("PERCENTAGE");
        line.UnitPrice.Should().Be(UnitPrice * 0.95m);
    }

    [Fact]
    public async Task AddLine_WithFixedAmountDiscount_SubtractsFlatValueAndFreezesFields()
    {
        var (warehouseToken, supplierToken, articleToken, _) = await SetupCatalogAsync("ARTDISC_FIXED");
        await Data.CreateArticleDiscountAsync(supplierToken, "FIXED_AMOUNT", 12.50m, DateTime.UtcNow.Date, articleToken: articleToken, currencyCode: "EUR");

        var line = await AddLineAndGetAsync(warehouseToken, articleToken);

        line.BaseUnitPrice.Should().Be(UnitPrice);
        line.DiscountTypeCode.Should().Be("FIXED_AMOUNT");
        line.DiscountValue.Should().Be(12.50m);
        line.UnitPrice.Should().Be(UnitPrice - 12.50m);
    }

    [Fact]
    public async Task AddLine_WithNoActiveDiscount_LeavesUnitPriceUnchangedAndBaseUnitPriceNull()
    {
        var (warehouseToken, _, articleToken, _) = await SetupCatalogAsync("ARTDISC_NONE");

        var line = await AddLineAndGetAsync(warehouseToken, articleToken);

        line.BaseUnitPrice.Should().BeNull();
        line.DiscountTypeCode.Should().BeNull();
        line.UnitPrice.Should().Be(UnitPrice);
    }

    [Fact]
    public async Task CreateDiscount_OverlappingSameScope_IsRejected()
    {
        var (_, supplierToken, articleToken, _) = await SetupCatalogAsync("ARTDISC_OVERLAP");
        var today = DateTime.UtcNow.Date;
        await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 10m, today, today.AddDays(10), articleToken: articleToken);

        var act = async () => await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 20m, today.AddDays(5), today.AddDays(15), articleToken: articleToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{ErrorCodes.ArticleDiscountOverlapping}*");
    }

    [Fact]
    public async Task CreateDiscount_FixedAmountWithoutCurrency_IsRejected()
    {
        var (_, supplierToken, articleToken, _) = await SetupCatalogAsync("ARTDISC_NOCURRENCY");

        var act = async () => await Data.CreateArticleDiscountAsync(supplierToken, "FIXED_AMOUNT", 5m, DateTime.UtcNow.Date, articleToken: articleToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{ErrorCodes.ArticleDiscountCurrencyRequired}*");
    }

    [Fact]
    public async Task Submit_WithActiveDiscount_FreezesDiscountOntoThePurchaseOrderLineToo()
    {
        // Regression guard: this exercises OrderService.CompleteSubmissionAsync's
        // BuildPurchaseOrderLineTable -> sp_PurchaseOrderLine_CreateBatch TVP round trip end to
        // end — the exact path that briefly broke when PurchaseOrderLineTableType gained 3 new
        // columns (BaseUnitPrice/DiscountTypeId/DiscountValue) but the C# DataTable builder
        // wasn't updated to match, found and fixed while wiring this feature.
        var (warehouseToken, supplierToken, articleToken, _) = await SetupCatalogAsync("ARTDISC_SUBMIT");
        await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 25m, DateTime.UtcNow.Date, articleToken: articleToken);

        var orderToken = await Data.CreateSubmittedOrderAsync(warehouseToken, articleToken, quantity: 1);
        var summary = await Data.GetSinglePurchaseOrderForOrderAsync(orderToken);
        // GetPurchaseOrdersQueryRequest (list/summary) doesn't populate Lines — need the detail
        // query, same as Data.ReceiveSingleLineAsync's own lookup.
        var detail = await Mediator.Send(new GetPurchaseOrderByTokenQueryRequest { PurchaseOrderToken = summary.PurchaseOrderToken });
        detail.Success.Should().BeTrue(string.Join("; ", detail.Errors.Select(e => $"{e.Code}: {e.Description}")));
        var line = detail.ReturnData!.PurchaseOrder.Lines.Single();

        line.BaseUnitPrice.Should().Be(UnitPrice);
        line.DiscountTypeCode.Should().Be("PERCENTAGE");
        line.DiscountValue.Should().Be(25m);
        line.UnitPrice.Should().Be(UnitPrice * 0.75m);
    }

    [Fact]
    public async Task Reactivate_WhenAnotherDiscountNowOccupiesTheSameOverlappingScope_IsRejected()
    {
        // Regression guard for a gap found in the 2026-08-07 full-system audit: CreateAsync/
        // EditAsync both re-validate overlap before writing, but SetActiveAsync didn't — so
        // deactivating discount A, creating overlapping discount B (allowed, since
        // sp_ArticleDiscount_GetByScope only ever sees IsActive=1 rows), then reactivating A would
        // silently resurrect an overlap CreateAsync itself would have hard-blocked with 409.
        var (_, supplierToken, articleToken, _) = await SetupCatalogAsync("ARTDISC_REACTIVATE");
        var today = DateTime.UtcNow.Date;

        var discountA = await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 10m, today, today.AddDays(10), articleToken: articleToken);

        var deactivate = await Mediator.Send(new SetActiveArticleDiscountCommandRequest { ArticleDiscountToken = discountA.ArticleDiscountToken, IsActive = false });
        deactivate.Success.Should().BeTrue(string.Join("; ", deactivate.Errors.Select(e => $"{e.Code}: {e.Description}")));

        // Now legal — A is inactive, so this doesn't collide per sp_ArticleDiscount_GetByScope.
        await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 20m, today.AddDays(5), today.AddDays(15), articleToken: articleToken);

        var reactivate = await Mediator.Send(new SetActiveArticleDiscountCommandRequest { ArticleDiscountToken = discountA.ArticleDiscountToken, IsActive = true });

        reactivate.Success.Should().BeFalse();
        reactivate.StatusCode.Should().Be(409);
        reactivate.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.ArticleDiscountOverlapping);
    }

    [Fact]
    public async Task Reactivate_WhenNoOtherDiscountOccupiesTheScope_Succeeds()
    {
        var (_, supplierToken, articleToken, _) = await SetupCatalogAsync("ARTDISC_REACTIVATE_OK");
        var today = DateTime.UtcNow.Date;

        var discount = await Data.CreateArticleDiscountAsync(supplierToken, "PERCENTAGE", 10m, today, today.AddDays(10), articleToken: articleToken);

        var deactivate = await Mediator.Send(new SetActiveArticleDiscountCommandRequest { ArticleDiscountToken = discount.ArticleDiscountToken, IsActive = false });
        deactivate.Success.Should().BeTrue();

        var reactivate = await Mediator.Send(new SetActiveArticleDiscountCommandRequest { ArticleDiscountToken = discount.ArticleDiscountToken, IsActive = true });

        reactivate.Success.Should().BeTrue();
        reactivate.ReturnData!.ArticleDiscount.IsActive.Should().BeTrue();
    }
}
