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
    /// (mirrors the simplest real articles in the catalog, e.g. "Cafe Tarrazu 1kg").</summary>
    public async Task<Guid> CreateArticleAsync(Guid supplierToken, Guid familyToken, string namePrefix)
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
            ]
        });
        return article.Article!.ArticleToken;
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
}
