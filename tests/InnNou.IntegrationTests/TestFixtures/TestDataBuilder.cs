using Dapper;
using InnNou.Application.Requests;
using InnNou.Application.Responses.Common;
using InnNou.Infrastructure.Abstractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace InnNou.IntegrationTests.TestFixtures;

/// <summary>
/// Builds test fixtures by calling the SAME real Create* commands the app itself uses (via
/// MediatR) - not raw SQL inserts. This re-verifies every entity's own create path on every
/// test run, exactly like a real user creating a supplier/article/order would, and it means a
/// test never has to guess what a hand-written INSERT needs to satisfy. Reference/lookup data
/// (Families, TaxCategories, TaxJurisdictions, UnitsOfMeasure, an ASSOCIATE Organization) is
/// looked up from what InnNou_Test already has - seeded/stable, never created by a test.
///
/// Every method here runs inside the calling test's TransactionScope (see
/// <see cref="TransactionalTestBase"/>) - nothing it creates ever persists.
/// </summary>
public class TestDataBuilder(IServiceProvider scopedProvider)
{
    private IMediator Mediator => scopedProvider.GetRequiredService<IMediator>();
    private IDbConnectionFactory ConnectionFactory => scopedProvider.GetRequiredService<IDbConnectionFactory>();

    private async Task<T> SendAsync<T>(MediatR.IRequest<InnNou.Application.Common.ApiResponse<T>> request)
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
            throw new InvalidOperationException(
                $"{request.GetType().Name} failed: {string.Join("; ", response.Errors.Select(e => $"{e.Code}: {e.Description}"))}");
        return response.ReturnData!;
    }

    /// <summary>Caps a generated name/code at <paramref name="maxLength"/> without throwing when
    /// it's already shorter - unlike a raw <c>[..maxLength]</c> range, which requires the string
    /// to be at least that long.</summary>
    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    // ── Reference/lookup data (seeded, never created by a test) ────────────────────────────

    public async Task<(Guid Token, int Id)> GetAssociateOrganizationAsync()
    {
        await using var connection = ConnectionFactory.CreateConnection();
        var row = await connection.QuerySingleAsync<(Guid OrganizationToken, int OrganizationId)>(
            @"SELECT TOP 1 o.OrganizationToken, o.OrganizationId
              FROM Organizations o
              JOIN OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
              WHERE ot.Code = 'ASSOCIATE' AND o.IsActive = 1 AND o.IsDeleted = 0
              ORDER BY o.OrganizationId");
        return (row.OrganizationToken, row.OrganizationId);
    }

    public async Task<Guid> GetUnitOfMeasureTokenAsync(string code)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>(
            "SELECT UnitOfMeasureToken FROM UnitsOfMeasure WHERE Code = @code", new { code });
    }

    public async Task<Guid> GetTaxCategoryTokenAsync(string code)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>(
            "SELECT TaxCategoryToken FROM TaxCategories WHERE Code = @code", new { code });
    }

    public async Task<Guid> GetTaxJurisdictionTokenAsync(string code)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>(
            "SELECT TaxJurisdictionToken FROM TaxJurisdictions WHERE Code = @code", new { code });
    }

    // ── Entities created via the real commands ──────────────────────────────────────────────

    /// <summary>A fresh Family with a unique code, defaulted to GENERAL - isolates each test
    /// from any FamilyTaxCategoryOverride another test (or a manual session) left behind.</summary>
    public async Task<Guid> CreateFamilyAsync(string namePrefix)
    {
        var family = await SendAsync(new CreateFamilyCommandRequest { Code = Truncate($"{namePrefix}_{Guid.NewGuid():N}", 30) });
        var generalToken = await GetTaxCategoryTokenAsync("GENERAL");
        var updated = await SendAsync(new SetFamilyDefaultTaxCategoryCommandRequest
        {
            FamilyToken = family.Family.FamilyToken,
            DefaultTaxCategoryToken = generalToken
        });
        return updated.Family.FamilyToken;
    }

    public async Task<Guid> CreateWarehouseAsync(Guid organizationToken, Guid taxJurisdictionToken, string namePrefix, bool canReceivePurchases = true)
    {
        var warehouse = await SendAsync(new CreateWarehouseCommandRequest
        {
            OrganizationToken = organizationToken,
            Name = Truncate($"{namePrefix} {Guid.NewGuid():N}", 40),
            TaxJurisdictionToken = taxJurisdictionToken,
            IsInventoriable = true,
            CanReceivePurchases = canReceivePurchases
        });
        return warehouse.WarehouseToken;
    }

    public async Task<Guid> CreateSupplierAsync(Guid organizationToken, string namePrefix)
    {
        var supplier = await SendAsync(new CreateSupplierCommandRequest
        {
            Name = Truncate($"{namePrefix} {Guid.NewGuid():N}", 200),
            IsGlobal = false,
            SupplierType = "PRODUCT",
            HasAccessToSystem = false,
            OrganizationToken = organizationToken
        });
        return supplier.SupplierToken;
    }

    /// <summary>A minimal 1-level article - purchase unit BOX, defined content 1 KILOGRAM
    /// (mirrors the simplest real articles in the catalog, e.g. "Cafe Tarrazu 1kg").
    /// <paramref name="taxCategoryToken"/> is optional (defaults to null, i.e. inherit the
    /// Family's own default) - pass it when a test needs a concrete, non-inherited starting
    /// value to later prove an override (e.g. Supersede's own TaxCategoryToken) actually took
    /// effect rather than just matching whatever the Family already defaults to.</summary>
    public async Task<Guid> CreateArticleAsync(Guid supplierToken, Guid familyToken, string namePrefix, Guid? taxCategoryToken = null)
    {
        var boxToken = await GetUnitOfMeasureTokenAsync("BOX");
        var kilogramToken = await GetUnitOfMeasureTokenAsync("KILOGRAM");

        var article = await SendAsync(new CreateArticleCommandRequest
        {
            SupplierToken = supplierToken,
            Name = Truncate($"{namePrefix} {Guid.NewGuid():N}", 200),
            FamilyToken = familyToken,
            PurchaseUnitToken = boxToken,
            PackagingLevels =
            [
                new ArticlePackagingLevelRequest
                {
                    SequenceOrder = 1,
                    UnitOfMeasureToken = kilogramToken,
                    QuantityInParentUnit = 1,
                    IsDefinedUnit = true
                }
            ],
            TaxCategoryToken = taxCategoryToken
        });
        return article.Article!.ArticleToken;
    }

    /// <summary>Supersedes an article with a genuine structural change (the defined-content
    /// quantity is always bumped from the original's 1 to 2, satisfying SupersedeArticleCommandHandler's
    /// own "must actually change something structural" guard) plus whatever optional overrides the
    /// caller supplies. Purchase unit stays BOX/defined-content stays KILOGRAM throughout - only
    /// the quantity moves - so callers don't need to know the original article's own packaging shape.</summary>
    public async Task<Article> SupersedeArticleAsync(Guid articleToken, string name, Guid? taxCategoryToken = null)
    {
        var boxToken = await GetUnitOfMeasureTokenAsync("BOX");
        var kilogramToken = await GetUnitOfMeasureTokenAsync("KILOGRAM");

        var result = await SendAsync(new SupersedeArticleCommandRequest
        {
            ArticleToken = articleToken,
            Name = name,
            PurchaseUnitToken = boxToken,
            PackagingLevels =
            [
                new ArticlePackagingLevelRequest
                {
                    SequenceOrder = 1,
                    UnitOfMeasureToken = kilogramToken,
                    QuantityInParentUnit = 2,
                    IsDefinedUnit = true
                }
            ],
            TaxCategoryToken = taxCategoryToken
        });
        return result.Article!;
    }

    public async Task CreateArticlePriceAsync(Guid articleToken, decimal price, string currencyCode = "EUR")
    {
        await SendAsync(new CreateArticlePriceCommandRequest
        {
            ArticleToken = articleToken,
            Price = price,
            CurrencyCode = currencyCode
        });
    }

    public async Task<Guid> CreateSubFamilyAsync(Guid familyToken, string namePrefix)
    {
        var subFamily = await SendAsync(new CreateSubFamilyCommandRequest
        {
            FamilyToken = familyToken,
            Code = Truncate($"{namePrefix}_{Guid.NewGuid():N}", 30)
        });
        return subFamily.SubFamily.SubFamilyToken;
    }

    // ── Article Discounts (per-Supplier promotional/time-bound pricing) ────────────────────

    public async Task<ArticleDiscount> CreateArticleDiscountAsync(
        Guid supplierToken,
        string discountTypeCode,
        decimal discountValue,
        DateTime effectiveFrom,
        DateTime? effectiveUntil = null,
        Guid? articleToken = null,
        Guid? subFamilyToken = null,
        Guid? familyToken = null,
        string? currencyCode = null)
    {
        var response = await SendAsync(new CreateArticleDiscountCommandRequest
        {
            SupplierToken = supplierToken,
            ArticleToken = articleToken,
            SubFamilyToken = subFamilyToken,
            FamilyToken = familyToken,
            DiscountTypeCode = discountTypeCode,
            DiscountValue = discountValue,
            CurrencyCode = currencyCode,
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = effectiveUntil
        });
        return response.ArticleDiscount;
    }

    // ── Order -> PurchaseOrder -> GoodsReceipt, one article, one line ──────────────────────

    public async Task<Guid> CreateSubmittedOrderAsync(Guid warehouseToken, Guid articleToken, decimal quantity)
    {
        var order = await SendAsync(new CreateOrderCommandRequest { WarehouseToken = warehouseToken });
        var orderToken = order.Order!.OrderToken;
        await SendAsync(new AddOrderLineCommandRequest
        {
            OrderToken = orderToken,
            ArticleToken = articleToken,
            Quantity = quantity
        });
        await SendAsync(new SubmitOrderCommandRequest { OrderToken = orderToken });
        return orderToken;
    }

    public async Task<PurchaseOrder> GetSinglePurchaseOrderForOrderAsync(Guid orderToken)
    {
        var result = await SendAsync(new GetPurchaseOrdersQueryRequest { OrderToken = orderToken, PageNumber = 1, PageSize = 10 });
        return result.PurchaseOrders.Single();
    }

    /// <summary>Records a receipt against the PO's single line with an arbitrary
    /// Accepted/Courtesy/Rejected split - the general-purpose entry point for GoodsReceipt tests.
    /// Assumes (like the rest of this builder) a PO with exactly one line.</summary>
    public async Task<GoodsReceipt> ReceiveSingleLineAsync(
        Guid purchaseOrderToken,
        string deliveryNoteNumber,
        decimal quantityAccepted = 0,
        decimal quantityCourtesy = 0,
        decimal quantityRejected = 0,
        string? rejectionReason = null)
    {
        var detail = await SendAsync(new GetPurchaseOrderByTokenQueryRequest { PurchaseOrderToken = purchaseOrderToken });
        var line = detail.PurchaseOrder.Lines.Single();

        var receipt = await SendAsync(new CreateGoodsReceiptCommandRequest
        {
            PurchaseOrderToken = purchaseOrderToken,
            DeliveryNoteNumber = deliveryNoteNumber,
            Lines =
            [
                new CreateGoodsReceiptLineRequestItem
                {
                    PurchaseOrderLineToken = line.PurchaseOrderLineToken,
                    QuantityAccepted = quantityAccepted,
                    QuantityCourtesy = quantityCourtesy,
                    QuantityRejected = quantityRejected,
                    RejectionReason = rejectionReason
                }
            ]
        });
        return receipt.GoodsReceipt!;
    }

    public Task<GoodsReceipt> ReceiveFullyAsync(Guid purchaseOrderToken, decimal quantity, string deliveryNoteNumber) =>
        ReceiveSingleLineAsync(purchaseOrderToken, deliveryNoteNumber, quantityAccepted: quantity);

    public async Task<string> GetPurchaseOrderStatusAsync(Guid purchaseOrderToken)
    {
        var detail = await SendAsync(new GetPurchaseOrderByTokenQueryRequest { PurchaseOrderToken = purchaseOrderToken });
        return detail.PurchaseOrder.Status;
    }

    // ── SupplierInvoice: matches a receipt's own frozen tax numbers against a buyer-typed
    // per-tax-rate VAT breakdown (Base Fra) ─────────────────────────────────────────────────

    public async Task UpsertSupplierInvoiceToleranceAsync(Guid organizationToken, decimal tolerancePercent, decimal toleranceAmount)
    {
        await SendAsync(new UpsertSupplierInvoiceMatchToleranceCommandRequest
        {
            OrganizationToken = organizationToken,
            TolerancePercent = tolerancePercent,
            ToleranceAmount = toleranceAmount
        });
    }

    public async Task UpsertSupplierInvoicePurchaseOrderPolicyAsync(Guid organizationToken, bool allowMultiplePurchaseOrders)
    {
        await SendAsync(new UpsertSupplierInvoicePurchaseOrderPolicyCommandRequest
        {
            OrganizationToken = organizationToken,
            AllowMultiplePurchaseOrders = allowMultiplePurchaseOrders
        });
    }

    /// <summary>Invoices every billable (QuantityAccepted &gt; 0) line of the given receipts in
    /// full - "a receipt is always invoiced in full" is enforced server-side, so this builder
    /// never supports a partial-line invoice. <paramref name="unitPrice"/> must match what the
    /// receipt's lines were actually priced at (this builder always uses one known price per
    /// article, set via <see cref="CreateArticlePriceAsync"/>, so the caller already knows it).
    /// A single tax-breakdown row is submitted per distinct <c>TaxRatePercent</c> across all
    /// billable lines, defaulting <c>BaseAmount</c> to the expected net total (sum of
    /// <c>TaxableAmount</c>) for that rate - pass <paramref name="baseAmountOverride"/> to submit
    /// a different (e.g. deliberately mismatched) number for a single-rate scenario instead.</summary>
    public async Task<SupplierInvoice> CreateSupplierInvoiceAsync(
        Guid organizationToken,
        Guid supplierToken,
        string invoiceNumber,
        List<GoodsReceipt> receipts,
        decimal unitPrice,
        decimal? baseAmountOverride = null)
    {
        var billableLines = receipts.SelectMany(r => r.Lines.Where(l => l.QuantityAccepted > 0)).ToList();

        var lines = billableLines.Select(l => new CreateSupplierInvoiceLineRequestItem
        {
            GoodsReceiptLineToken = l.GoodsReceiptLineToken,
            QuantityInvoiced = l.QuantityAccepted,
            UnitPriceInvoiced = unitPrice
        }).ToList();

        var breakdown = billableLines
            .GroupBy(l => l.TaxRatePercent)
            .Select(g => new CreateSupplierInvoiceTaxBreakdownRequestItem
            {
                TaxRatePercent = g.Key,
                BaseAmount = baseAmountOverride ?? g.Sum(l => l.TaxableAmount!.Value)
            })
            .ToList();

        var response = await SendAsync(new CreateSupplierInvoiceCommandRequest
        {
            OrganizationToken = organizationToken,
            SupplierToken = supplierToken,
            SupplierInvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.UtcNow.Date,
            GoodsReceiptTokens = receipts.Select(r => r.GoodsReceiptToken).ToList(),
            Lines = lines,
            TaxBreakdown = breakdown
        });
        return response.SupplierInvoice;
    }

    // ── Order Approval Workflow: per-Family spend thresholds, sequential designated approvers ─

    /// <summary>A fresh User within the given Organization, usable as a
    /// <see cref="CreateFamilyApprovalThresholdCommandRequest.ApproverUserToken"/> and then as the
    /// caller identity (<c>TestRequestContext.ActorUserToken</c>/<c>EffectiveUserToken</c>) when a
    /// test acts as that designated approver. The Role assigned is arbitrary - the approval flow's
    /// "is this caller the designated approver" check compares the resolved UserId only, never the
    /// user's own RoleLevel (that's driven entirely by the test's own TestRequestContext).</summary>
    public async Task<Guid> CreateApproverUserAsync(int organizationId, string namePrefix)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        // A caller can only assign a Role at or below their own RoleLevel (UserService.CreateUserAsync) -
        // every test in this builder acts as an Admin (RoleLevel 80), so pick a Staff-range role
        // (<= 20) to stay safely under that ceiling regardless of exactly which roles are seeded.
        var roleId = await connection.QuerySingleAsync<int>(
            "SELECT TOP 1 RoleId FROM Roles WHERE IsActive = 1 AND RoleLevel <= 20 ORDER BY RoleLevel DESC");

        var user = await SendAsync(new CreateUserCommandRequest
        {
            Email = $"{Guid.NewGuid():N}@grtest.invalid",
            Password = "TestPassword123!",
            FirstName = Truncate(namePrefix, 50),
            LastName = "Approver",
            UserName = Truncate($"{namePrefix}_{Guid.NewGuid():N}", 50),
            RoleId = roleId,
            OrganizationId = organizationId
        });
        return user.UserToken;
    }

    /// <summary>Levels must be created strictly in order starting at 1, with each
    /// ThresholdAmount exceeding the previous level's - enforced server-side, so a test building a
    /// multi-level threshold must call this once per level, lowest first.</summary>
    public async Task CreateFamilyApprovalThresholdAsync(Guid organizationToken, Guid familyToken, int level, decimal thresholdAmount, Guid approverUserToken)
    {
        await SendAsync(new CreateFamilyApprovalThresholdCommandRequest
        {
            OrganizationToken = organizationToken,
            FamilyToken = familyToken,
            Level = level,
            ThresholdAmount = thresholdAmount,
            ApproverUserToken = approverUserToken
        });
    }

    public async Task<Order> GetOrderAsync(Guid orderToken)
    {
        var response = await SendAsync(new GetOrderByTokenQueryRequest { OrderToken = orderToken });
        return response.Order;
    }

    public async Task<OrderApprovalStep> ApproveOrderApprovalStepAsync(Guid orderApprovalStepToken)
    {
        var response = await SendAsync(new ApproveOrderApprovalStepCommandRequest { OrderApprovalStepToken = orderApprovalStepToken });
        return response.OrderApprovalStep;
    }

    public async Task<OrderApprovalStep> RejectOrderApprovalStepAsync(Guid orderApprovalStepToken, string reason)
    {
        var response = await SendAsync(new RejectOrderApprovalStepCommandRequest { OrderApprovalStepToken = orderApprovalStepToken, Reason = reason });
        return response.OrderApprovalStep;
    }
}
