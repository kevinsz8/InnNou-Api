using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class SupplierInvoiceService(
    IDbConnectionFactory connectionFactory,
    IMapper mapper,
    IPurchaseOrderService purchaseOrderService,
    ISupplierInvoiceFileStorage fileStorage) : ISupplierInvoiceService
{
    private sealed class SupplierInvoicePageRow : SupplierInvoice { public int TotalCount { get; set; } }

    // Cargar una factura toca numeros de pago reales, un escalon mas alto que Recepcion de
    // mercaderia (Staff+) — confirmado explicitamente con el usuario, ver
    // .claude/GoodsReceiptsModule.md's Facturacion section.
    private const int AdminRoleLevel = 80;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // Mirrors PurchaseOrderService's own CanManageOrganizationAsync exactly (ASSOCIATE-only,
    // no bare-SuperAdmin bypass — must impersonate a real Asociado-org user), but at
    // AdminRoleLevel instead of Staff.
    private static async Task<bool> CanManageSupplierInvoicesAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            return false;

        if (context.RoleLevel < AdminRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static async Task<bool> CanReadOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private async Task HydrateAsync(IDbConnection connection, SupplierInvoiceDto dto, int supplierInvoiceId)
    {
        dto.Lines = mapper.MapList<SupplierInvoiceLineDto>(
            await connection.QueryAsync<SupplierInvoiceLine>(
                "sp_SupplierInvoiceLine_GetBySupplierInvoiceId", new { SupplierInvoiceId = supplierInvoiceId }, commandType: CommandType.StoredProcedure));

        dto.PurchaseOrders = mapper.MapList<SupplierInvoicePurchaseOrderDto>(
            await connection.QueryAsync<SupplierInvoicePurchaseOrder>(
                "sp_SupplierInvoicePurchaseOrder_GetBySupplierInvoiceId", new { SupplierInvoiceId = supplierInvoiceId }, commandType: CommandType.StoredProcedure));

        dto.TaxBreakdown = mapper.MapList<SupplierInvoiceTaxBreakdownDto>(
            await connection.QueryAsync<SupplierInvoiceTaxBreakdown>(
                "sp_SupplierInvoiceTaxBreakdown_GetBySupplierInvoiceId", new { SupplierInvoiceId = supplierInvoiceId }, commandType: CommandType.StoredProcedure));

        dto.LineCount = dto.Lines.Count;
    }

    // sp_SupplierInvoice_GetByToken's own SELECT doesn't compute these (only
    // sp_SupplierInvoice_GetPaged's CROSS APPLY does) — derive them here from the
    // already-hydrated Lines/PurchaseOrders instead of duplicating the aggregation in SQL a
    // second time. Used by both GetByTokenAsync and CreateAsync (which reads back through the
    // same GetByToken-equivalent shape right after inserting).
    private static void PopulateComputedTotals(SupplierInvoiceDto dto)
    {
        dto.TotalTaxableAmount = dto.Lines.Sum(l => l.TaxableAmount);
        dto.TotalAmount = dto.Lines.Sum(l => l.TotalAmount);
        dto.PurchaseOrderNumbers = string.Join(", ", dto.PurchaseOrders.Select(po => po.PurchaseOrderNumber));
        dto.WarehouseNames = string.Join(", ", dto.Lines.Select(l => l.WarehouseName).Where(w => !string.IsNullOrWhiteSpace(w)).Distinct());
    }

    public async Task<PagedResult<SupplierInvoiceDto>> GetPagedAsync(Guid? organizationToken, Guid? supplierToken, string? status, string? searchText, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Resolves the hierarchy root the SP's recursive CTE expands from — same shape as
        // PurchaseOrderService.GetPagedAsync. An explicit organizationToken always wins (lets
        // even a bare SuperAdmin narrow to one org). Omitting it falls back to a whole-hierarchy
        // search, but — unlike PurchaseOrder — that fallback is deliberately restricted to
        // non-ASSOCIATE callers (SuperAdmin/Admin/Super Asociado): confirmed with the user that
        // an ASSOCIATE (single-property) caller must always pick their own organization
        // explicitly, since "browse every property's invoices at once" has no use case for them.
        int? rootOrganizationId;

        if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken", new { OrganizationToken = organizationToken.Value, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);

            if (organization is null)
                return new PagedResult<SupplierInvoiceDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            if (!await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                throw new ApiException(ErrorCodes.SupplierInvoiceForbidden, "Cannot view supplier invoices outside your scope.", 403);

            rootOrganizationId = organization.OrganizationId;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            // Bare SuperAdmin (no organization of their own) => null => fully unrestricted;
            // an impersonating SuperAdmin => their impersonated org's own hierarchy.
            rootOrganizationId = context.OrganizationId;
        }
        else if (context.OrganizationTypeCode != OrganizationTypeCodes.Associate && context.OrganizationId.HasValue)
        {
            rootOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            throw new ApiException(ErrorCodes.InvalidRequest, "An organization must be selected.", 400);
        }

        int? supplierId = null;
        if (supplierToken.HasValue)
        {
            var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
                "sp_Supplier_GetByToken", new { SupplierToken = supplierToken.Value }, commandType: CommandType.StoredProcedure);
            supplierId = supplier?.SupplierId;
        }

        int? statusId = null;
        if (SupplierInvoiceStatusCodes.TryFromCode(status, out var parsedStatus))
            statusId = (int)parsedStatus;

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@StatusId", statusId);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<SupplierInvoicePageRow>(
            "sp_SupplierInvoice_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<SupplierInvoiceDto>
        {
            Items = mapper.MapList<SupplierInvoiceDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<SupplierInvoiceDto?> GetByTokenAsync(Guid supplierInvoiceToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var row = await connection.QueryFirstOrDefaultAsync<SupplierInvoice>(
            "sp_SupplierInvoice_GetByToken", new { SupplierInvoiceToken = supplierInvoiceToken }, commandType: CommandType.StoredProcedure);

        if (row is null)
            return null;

        if (!await CanReadOrganizationAsync(connection, context, row.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceForbidden, "Cannot view a supplier invoice outside your scope.", 403);

        var dto = mapper.Map<SupplierInvoiceDto>(row);
        await HydrateAsync(connection, dto, row.SupplierInvoiceId);
        PopulateComputedTotals(dto);
        return dto;
    }

    public async Task<PagedResult<GoodsReceiptForInvoicingDto>> GetEligibleGoodsReceiptsForInvoicingAsync(Guid organizationToken, Guid supplierToken, string? purchaseOrderNumber, string? deliveryNoteNumber, DateTime? fromDate, DateTime? toDate, string? dateType, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (organization is null)
            return new PagedResult<GoodsReceiptForInvoicingDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        if (!await CanManageSupplierInvoicesAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceForbidden, "Cannot browse goods receipts outside your scope.", 403);

        var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken", new { SupplierToken = supplierToken }, commandType: CommandType.StoredProcedure);
        if (supplier is null)
            return new PagedResult<GoodsReceiptForInvoicingDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@SupplierId", supplier.SupplierId);
        p.Add("@PurchaseOrderNumber", string.IsNullOrWhiteSpace(purchaseOrderNumber) ? null : purchaseOrderNumber.Trim());
        p.Add("@DeliveryNoteNumber", string.IsNullOrWhiteSpace(deliveryNoteNumber) ? null : deliveryNoteNumber.Trim());
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@DateType", string.IsNullOrWhiteSpace(dateType) ? null : dateType.Trim());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<GoodsReceiptForInvoicing>(
            "sp_GoodsReceipt_GetEligibleForInvoicing", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<GoodsReceiptForInvoicingDto>
        {
            Items = mapper.MapList<GoodsReceiptForInvoicingDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<SupplierInvoiceMatchToleranceDto?> GetEffectiveToleranceAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (organization is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceOrganizationNotFound, "Organization not found.", 404);

        if (!await CanManageSupplierInvoicesAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceToleranceForbidden, "Cannot view tolerance configuration outside your scope.", 403);

        var row = await connection.QueryFirstOrDefaultAsync<SupplierInvoiceMatchTolerance>(
            "sp_SupplierInvoiceMatchTolerance_GetEffective", new { organization.OrganizationId }, commandType: CommandType.StoredProcedure);

        return row is null ? null : mapper.Map<SupplierInvoiceMatchToleranceDto>(row);
    }

    public async Task<SupplierInvoiceMatchToleranceDto?> UpsertToleranceAsync(Guid organizationToken, decimal tolerancePercent, decimal toleranceAmount, IRequestContext context, CancellationToken cancellationToken)
    {
        if (tolerancePercent < 0 || tolerancePercent > 100)
            throw new ApiException(ErrorCodes.SupplierInvoiceToleranceInvalid, "The tolerance percent must be between 0 and 100.", 400);

        if (toleranceAmount < 0)
            throw new ApiException(ErrorCodes.SupplierInvoiceToleranceInvalid, "The tolerance amount cannot be negative.", 400);

        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (organization is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceOrganizationNotFound, "Organization not found.", 404);

        if (!await CanManageSupplierInvoicesAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceToleranceForbidden, "Cannot configure tolerance outside your scope.", 403);

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@TolerancePercent", tolerancePercent);
        p.Add("@ToleranceAmount", toleranceAmount);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        var row = await connection.QueryFirstOrDefaultAsync<SupplierInvoiceMatchTolerance>(
            "sp_SupplierInvoiceMatchTolerance_Upsert", p, commandType: CommandType.StoredProcedure);

        return row is null ? null : mapper.Map<SupplierInvoiceMatchToleranceDto>(row);
    }

    public async Task<SupplierInvoicePurchaseOrderPolicyDto?> GetEffectivePurchaseOrderPolicyAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (organization is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceOrganizationNotFound, "Organization not found.", 404);

        if (!await CanManageSupplierInvoicesAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderPolicyForbidden, "Cannot view purchase order policy outside your scope.", 403);

        return await GetEffectivePurchaseOrderPolicyAsync(connection, organization.OrganizationId);
    }

    // Shared by the public GetEffectivePurchaseOrderPolicyAsync (already-authorized read for the
    // settings panel) and CreateAsync's own enforcement check below — both need the same
    // resolved policy, but CreateAsync already has an open connection/transaction and an
    // already-resolved OrganizationId, so it skips the token round-trip and auth re-check.
    private async Task<SupplierInvoicePurchaseOrderPolicyDto?> GetEffectivePurchaseOrderPolicyAsync(IDbConnection connection, int organizationId)
    {
        var row = await connection.QueryFirstOrDefaultAsync<SupplierInvoicePurchaseOrderPolicy>(
            "sp_SupplierInvoicePurchaseOrderPolicy_GetEffective", new { OrganizationId = organizationId }, commandType: CommandType.StoredProcedure);

        return row is null ? null : mapper.Map<SupplierInvoicePurchaseOrderPolicyDto>(row);
    }

    public async Task<SupplierInvoicePurchaseOrderPolicyDto?> UpsertPurchaseOrderPolicyAsync(Guid organizationToken, bool allowMultiplePurchaseOrders, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (organization is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceOrganizationNotFound, "Organization not found.", 404);

        if (!await CanManageSupplierInvoicesAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderPolicyForbidden, "Cannot configure purchase order policy outside your scope.", 403);

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@AllowMultiplePurchaseOrders", allowMultiplePurchaseOrders);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        var row = await connection.QueryFirstOrDefaultAsync<SupplierInvoicePurchaseOrderPolicy>(
            "sp_SupplierInvoicePurchaseOrderPolicy_Upsert", p, commandType: CommandType.StoredProcedure);

        return row is null ? null : mapper.Map<SupplierInvoicePurchaseOrderPolicyDto>(row);
    }

    // One resolved, invoiceable line — the effective PurchaseOrderLine values (what was
    // ordered/its rectified price) paired against the specific GoodsReceiptLine's own
    // QuantityAccepted (what was actually received in THAT delivery — not the PO's full
    // ordered quantity, since 2026-08-02 invoicing is per-receipt, not per-PurchaseOrder), plus
    // the caller-supplied (possibly corrected) invoiced values.
    private sealed record ResolvedLine(
        int PurchaseOrderLineId,
        int GoodsReceiptLineId,
        int PurchaseOrderId,
        int ArticleId,
        int WarehouseId,
        Guid WarehouseToken,
        string CurrencyCode,
        decimal ExpectedQuantity,
        decimal ExpectedUnitPrice,
        decimal QuantityInvoiced,
        decimal UnitPriceInvoiced,
        // Tax already frozen at receipt time (GoodsReceiptLine.TaxCategoryId/TaxRatePercent/
        // TaxableAmount/TaxAmount) — reused as-is here, never re-resolved live, same
        // freeze-and-never-recompute discipline every other snapshot in this codebase follows.
        int? TaxCategoryId,
        decimal? TaxRatePercent,
        decimal? FrozenTaxableAmount,
        decimal? FrozenTaxAmount);

    public async Task<SupplierInvoiceDto?> CreateAsync(Guid organizationToken, Guid supplierToken, string supplierInvoiceNumber, DateTime invoiceDate, string? notes, List<Guid> goodsReceiptTokens, List<CreateSupplierInvoiceLineInputDto> lines, List<CreateSupplierInvoiceTaxBreakdownInputDto> taxBreakdown, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (organization is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceOrganizationNotFound, "Organization not found.", 404);

        if (!await CanManageSupplierInvoicesAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceForbidden, "Cannot create a supplier invoice outside your scope.", 403);

        var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken", new { SupplierToken = supplierToken }, commandType: CommandType.StoredProcedure);
        if (supplier is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceSupplierNotFound, "Supplier not found.", 404);

        if (goodsReceiptTokens.Count == 0)
            throw new ApiException(ErrorCodes.SupplierInvoiceEmpty, "At least one goods receipt must be selected.", 400);

        // Resolve + validate each selected GoodsReceipt (delivery/albarán), and collect every
        // one of its billable lines (QuantityAccepted > 0) as the set the submitted lines must
        // match exactly (see the count check below) — a receipt is invoiced entirely in one
        // shot, same "all or nothing" rule the old PO-level flow had, just at receipt
        // granularity now. goodsReceiptIdByToken/purchaseOrderTokenById feed the transaction
        // below (exclusivity inserts, PO status advance) without re-fetching.
        var expectedLines = new Dictionary<Guid, ResolvedLine>();
        var goodsReceiptIdByToken = new Dictionary<Guid, int>();
        var purchaseOrderTokenById = new Dictionary<int, Guid>();
        var purchaseOrderStatusById = new Dictionary<int, string>();

        foreach (var goodsReceiptToken in goodsReceiptTokens)
        {
            var goodsReceipt = await connection.QueryFirstOrDefaultAsync<GoodsReceiptWithPurchaseOrderContext>(
                "sp_GoodsReceipt_GetByToken", new { GoodsReceiptToken = goodsReceiptToken }, commandType: CommandType.StoredProcedure);
            if (goodsReceipt is null)
                throw new ApiException(ErrorCodes.SupplierInvoiceGoodsReceiptNotFound, $"Goods receipt '{goodsReceiptToken}' not found.", 404);

            if (goodsReceipt.OrganizationId != organization.OrganizationId)
                throw new ApiException(ErrorCodes.SupplierInvoiceGoodsReceiptNotFound, $"The goods receipt for purchase order '{goodsReceipt.PurchaseOrderNumber}' does not belong to this organization.", 404);

            if (goodsReceipt.SupplierId != supplier.SupplierId)
                throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderDifferentSupplier, $"The goods receipt for purchase order '{goodsReceipt.PurchaseOrderNumber}' belongs to a different supplier.", 400);

            if (goodsReceipt.PurchaseOrderStatus != PurchaseOrderStatusCodes.PartiallyReceived && goodsReceipt.PurchaseOrderStatus != PurchaseOrderStatusCodes.Received)
                throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderNotReceived, $"Purchase order '{goodsReceipt.PurchaseOrderNumber}' has no confirmed delivery to invoice.", 409);

            goodsReceiptIdByToken[goodsReceiptToken] = goodsReceipt.GoodsReceiptId;
            purchaseOrderTokenById[goodsReceipt.PurchaseOrderId] = goodsReceipt.PurchaseOrderToken;
            purchaseOrderStatusById[goodsReceipt.PurchaseOrderId] = goodsReceipt.PurchaseOrderStatus;

            var purchaseOrder = await purchaseOrderService.GetByTokenAsync(goodsReceipt.PurchaseOrderToken, context, cancellationToken);
            if (purchaseOrder is null)
                throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderNotFound, $"Purchase order '{goodsReceipt.PurchaseOrderNumber}' not found.", 404);

            var purchaseOrderLinesById = purchaseOrder.Lines.ToDictionary(l => l.PurchaseOrderLineId);

            var receiptLines = await connection.QueryAsync<GoodsReceiptLine>(
                "sp_GoodsReceiptLine_GetByGoodsReceiptId", new { goodsReceipt.GoodsReceiptId }, commandType: CommandType.StoredProcedure);

            foreach (var receiptLine in receiptLines.Where(l => l.QuantityAccepted > 0))
            {
                // FK-guaranteed to exist on this same PO — defensive skip rather than throw.
                if (!purchaseOrderLinesById.TryGetValue(receiptLine.PurchaseOrderLineId, out var purchaseOrderLine))
                    continue;

                expectedLines[receiptLine.GoodsReceiptLineToken] = new ResolvedLine(
                    PurchaseOrderLineId: receiptLine.PurchaseOrderLineId,
                    GoodsReceiptLineId: receiptLine.GoodsReceiptLineId,
                    PurchaseOrderId: goodsReceipt.PurchaseOrderId,
                    ArticleId: purchaseOrderLine.ArticleId,
                    WarehouseId: purchaseOrder.WarehouseId,
                    WarehouseToken: purchaseOrder.WarehouseToken,
                    CurrencyCode: purchaseOrderLine.CurrencyCode,
                    ExpectedQuantity: receiptLine.QuantityAccepted,
                    ExpectedUnitPrice: purchaseOrderLine.UnitPrice,
                    QuantityInvoiced: 0,
                    UnitPriceInvoiced: 0,
                    TaxCategoryId: receiptLine.TaxCategoryId,
                    TaxRatePercent: receiptLine.TaxRatePercent,
                    FrozenTaxableAmount: receiptLine.TaxableAmount,
                    FrozenTaxAmount: receiptLine.TaxAmount);
            }
        }

        if (purchaseOrderTokenById.Count > 1)
        {
            // Absence of any configured policy in the organization's ancestry defaults to
            // "allowed" — see sp_SupplierInvoicePurchaseOrderPolicy_GetEffective's own comment.
            // Counts DISTINCT purchase orders touched, not receipts — two receipts of the same
            // PO are not "multiple purchase orders." Re-checked here even though the frontend
            // already renders a single-select radio picker when this is disabled — never trust
            // the frontend already filtered it.
            var policy = await GetEffectivePurchaseOrderPolicyAsync(connection, organization.OrganizationId);
            if (policy is not null && !policy.AllowMultiplePurchaseOrders)
                throw new ApiException(ErrorCodes.SupplierInvoiceMultiplePurchaseOrdersNotAllowed, "This organization's policy only allows one purchase order per invoice.", 409);
        }

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.SupplierInvoiceEmpty, "At least one line must be invoiced.", 400);

        var submittedTokens = new HashSet<Guid>();
        var resolvedLines = new List<ResolvedLine>();

        foreach (var input in lines)
        {
            if (!expectedLines.TryGetValue(input.GoodsReceiptLineToken, out var expected))
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, $"Line '{input.GoodsReceiptLineToken}' does not belong to any of the selected goods receipts.", 400);

            if (!submittedTokens.Add(input.GoodsReceiptLineToken))
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, $"Line '{input.GoodsReceiptLineToken}' was submitted more than once.", 400);

            if (input.QuantityInvoiced <= 0)
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, "Invoiced quantity must be greater than zero.", 400);

            if (input.UnitPriceInvoiced < 0)
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, "Invoiced unit price cannot be negative.", 400);

            resolvedLines.Add(expected with { QuantityInvoiced = input.QuantityInvoiced, UnitPriceInvoiced = input.UnitPriceInvoiced });
        }

        if (submittedTokens.Count != expectedLines.Count)
            throw new ApiException(ErrorCodes.SupplierInvoiceLineIncomplete, "Every billable line of every selected goods receipt must be invoiced — a receipt is always invoiced in full.", 400);

        // Tolerance — a hard requirement, unlike tax below (matching cannot be computed without it).
        var tolerance = await connection.QueryFirstOrDefaultAsync<SupplierInvoiceMatchTolerance>(
            "sp_SupplierInvoiceMatchTolerance_GetEffective", new { organization.OrganizationId }, commandType: CommandType.StoredProcedure);
        if (tolerance is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceToleranceNotConfigured, "No invoice-matching tolerance is configured for this organization or any of its ancestors — configure one before creating a supplier invoice.", 400);

        // Tax is reused as-is from what was already frozen on each GoodsReceiptLine at receipt
        // time (ResolvedLine.TaxCategoryId/TaxRatePercent) — never re-resolved live here, same
        // freeze-and-never-recompute discipline every other snapshot in this codebase follows.

        // "Base Fra" per tax rate — typed by the caller from the supplier's real invoice, the
        // source of truth for MATCHED/DISCREPANCY now that per-line quantity/price can no longer
        // diverge from the receipt (see CreateSupplierInvoiceTaxBreakdownInputDto). Validated,
        // never blocking — an out-of-tolerance total still saves, just flagged.
        if (taxBreakdown.Count == 0)
            throw new ApiException(ErrorCodes.SupplierInvoiceTaxBreakdownRequired, "At least one tax-rate breakdown row (Base Fra) is required, transcribed from the supplier's real invoice.", 400);

        foreach (var breakdown in taxBreakdown)
        {
            if (breakdown.BaseAmount < 0)
                throw new ApiException(ErrorCodes.SupplierInvoiceTaxBreakdownInvalid, "Tax breakdown base amount cannot be negative.", 400);

            if (breakdown.TaxRatePercent is < 0)
                throw new ApiException(ErrorCodes.SupplierInvoiceTaxBreakdownInvalid, "Tax breakdown rate cannot be negative.", 400);
        }

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@SupplierInvoiceToken", Guid.NewGuid());
            headerParams.Add("@OrganizationId", organization.OrganizationId);
            headerParams.Add("@SupplierId", supplier.SupplierId);
            headerParams.Add("@SupplierInvoiceNumber", supplierInvoiceNumber);
            headerParams.Add("@InvoiceDate", invoiceDate.Date);
            headerParams.Add("@SupplierInvoiceStatusId", (int)SupplierInvoiceStatus.Matched);
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<SupplierInvoice>(
                "sp_SupplierInvoice_Create", headerParams, transaction, commandType: CommandType.StoredProcedure);

            if (header is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            foreach (var line in resolvedLines)
            {
                var taxableAmount = line.QuantityInvoiced * line.UnitPriceInvoiced;
                var expectedNetAmount = line.ExpectedQuantity * line.ExpectedUnitPrice;
                var amountDiff = Math.Abs(taxableAmount - expectedNetAmount);
                var percentDiff = expectedNetAmount == 0
                    ? (taxableAmount == 0 ? 0 : 100)
                    : amountDiff / expectedNetAmount * 100;
                // Kept for the line's own audit record even though it's now structurally always
                // true (QuantityInvoiced/UnitPriceInvoiced can no longer diverge from the receipt,
                // see CreateSupplierInvoiceLineInputDto) — no longer drives the header status,
                // see the tax-breakdown-based check below instead.
                var isWithinTolerance = percentDiff <= tolerance.TolerancePercent && amountDiff <= tolerance.ToleranceAmount;

                var taxAmount = line.FrozenTaxAmount;
                var totalAmount = taxableAmount + (taxAmount ?? 0);

                var lineParams = new DynamicParameters();
                lineParams.Add("@SupplierInvoiceLineToken", Guid.NewGuid());
                lineParams.Add("@SupplierInvoiceId", header.SupplierInvoiceId);
                lineParams.Add("@PurchaseOrderLineId", line.PurchaseOrderLineId);
                lineParams.Add("@GoodsReceiptLineId", line.GoodsReceiptLineId);
                lineParams.Add("@ArticleId", line.ArticleId);
                lineParams.Add("@QuantityInvoiced", line.QuantityInvoiced);
                lineParams.Add("@UnitPriceInvoiced", line.UnitPriceInvoiced);
                lineParams.Add("@CurrencyCode", line.CurrencyCode);
                lineParams.Add("@TaxCategoryId", line.TaxCategoryId);
                lineParams.Add("@TaxRatePercent", line.TaxRatePercent);
                lineParams.Add("@TaxableAmount", taxableAmount);
                lineParams.Add("@TaxAmount", taxAmount);
                lineParams.Add("@TotalAmount", totalAmount);
                lineParams.Add("@IsWithinTolerance", isWithinTolerance);
                lineParams.Add("@CreatedBy", actor);

                try
                {
                    await connection.ExecuteAsync("sp_SupplierInvoiceLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
                }
                catch (SqlException ex) when (ex.Number is 2601 or 2627)
                {
                    throw new ApiException(ErrorCodes.SupplierInvoiceGoodsReceiptAlreadyInvoiced, "One of the selected goods receipt lines was already invoiced by another request.", 409);
                }
            }

            // Tax-breakdown rows — "Base Fra" per rate, transcribed from the real invoice.
            foreach (var breakdown in taxBreakdown)
            {
                var breakdownTaxAmount = breakdown.TaxRatePercent.HasValue
                    ? Math.Round(breakdown.BaseAmount * breakdown.TaxRatePercent.Value / 100m, 8)
                    : 0m;

                await connection.ExecuteAsync(
                    "sp_SupplierInvoiceTaxBreakdown_Create",
                    new
                    {
                        SupplierInvoiceTaxBreakdownToken = Guid.NewGuid(),
                        header.SupplierInvoiceId,
                        breakdown.TaxRatePercent,
                        breakdown.BaseAmount,
                        TaxAmount = breakdownTaxAmount,
                        CreatedBy = actor
                    },
                    transaction, commandType: CommandType.StoredProcedure);
            }

            // MATCHED/DISCREPANCY is now decided by comparing what the user transcribed from the
            // real invoice (SUM of Base Fra) against what the selected receipts' own net amount
            // adds up to — the per-line check above can no longer surface a mismatch on its own
            // now that quantity/price are fixed by the receipt. Out-of-tolerance still saves,
            // just flagged (matches the industry-standard "allow with warning" default, not a
            // hard block — researched before building).
            var expectedNetTotal = resolvedLines.Sum(l => l.ExpectedQuantity * l.ExpectedUnitPrice);
            var typedBaseTotal = taxBreakdown.Sum(b => b.BaseAmount);
            var headerAmountDiff = Math.Abs(typedBaseTotal - expectedNetTotal);
            var headerPercentDiff = expectedNetTotal == 0
                ? (typedBaseTotal == 0 ? 0 : 100)
                : headerAmountDiff / expectedNetTotal * 100;
            var anyDiscrepancy = headerPercentDiff > tolerance.TolerancePercent || headerAmountDiff > tolerance.ToleranceAmount;

            // Exclusivity gate: each selected GoodsReceipt (not PurchaseOrder — a PO can now
            // legitimately span several invoices, one per delivery) can be invoiced at most once.
            foreach (var (goodsReceiptToken, goodsReceiptId) in goodsReceiptIdByToken)
            {
                try
                {
                    await connection.ExecuteAsync(
                        "sp_SupplierInvoiceGoodsReceipt_Create",
                        new { header.SupplierInvoiceId, GoodsReceiptId = goodsReceiptId, CreatedBy = actor },
                        transaction, commandType: CommandType.StoredProcedure);
                }
                catch (SqlException ex) when (ex.Number is 2601 or 2627)
                {
                    throw new ApiException(ErrorCodes.SupplierInvoiceGoodsReceiptAlreadyInvoiced, "One of the selected goods receipts was already invoiced by another request.", 409);
                }
            }

            // "PEDIDOS DE COMPRA CONSOLIDADOS" display chips — once per DISTINCT PurchaseOrder
            // touched (never twice within the same invoice, even if two of its receipts were
            // both selected here); no longer the exclusivity gate itself, see above.
            foreach (var purchaseOrderId in purchaseOrderTokenById.Keys)
            {
                await connection.ExecuteAsync(
                    "sp_SupplierInvoicePurchaseOrder_Create",
                    new { header.SupplierInvoiceId, PurchaseOrderId = purchaseOrderId },
                    transaction, commandType: CommandType.StoredProcedure);
            }

            // Advance a touched PO to INVOICED only once it's fully RECEIVED (no more deliveries
            // expected) AND every one of its GoodsReceipts — not just the ones selected in this
            // request — has now been invoiced. A PARTIALLY_RECEIVED PO (this invoice covered one
            // of its deliveries, more are still expected) is deliberately left as-is: more
            // invoicing activity may still happen against it later, same real-world practice as
            // SAP's Goods-Receipt-Based Invoice Verification.
            foreach (var purchaseOrderId in purchaseOrderTokenById.Keys)
            {
                if (purchaseOrderStatusById[purchaseOrderId] != PurchaseOrderStatusCodes.Received)
                    continue;

                var hasUninvoicedGoodsReceipts = await connection.ExecuteScalarAsync<bool>(
                    "sp_GoodsReceipt_ExistsUninvoicedForPurchaseOrder", new { PurchaseOrderId = purchaseOrderId },
                    transaction, commandType: CommandType.StoredProcedure);

                if (!hasUninvoicedGoodsReceipts)
                {
                    await connection.ExecuteAsync(
                        "sp_PurchaseOrder_SetStatus",
                        new { PurchaseOrderToken = purchaseOrderTokenById[purchaseOrderId], Status = PurchaseOrderStatusCodes.Invoiced },
                        transaction, commandType: CommandType.StoredProcedure);
                }
            }

            if (anyDiscrepancy)
            {
                var statusParams = new DynamicParameters();
                statusParams.Add("@SupplierInvoiceToken", header.SupplierInvoiceToken);
                statusParams.Add("@SupplierInvoiceStatusId", (int)SupplierInvoiceStatus.Discrepancy);
                await connection.ExecuteAsync(
                    "UPDATE dbo.SupplierInvoices SET SupplierInvoiceStatusId = @SupplierInvoiceStatusId WHERE SupplierInvoiceToken = @SupplierInvoiceToken",
                    statusParams, transaction);
            }

            await transaction.CommitAsync(cancellationToken);

            var dto = mapper.Map<SupplierInvoiceDto>(header);
            dto.Status = anyDiscrepancy ? SupplierInvoiceStatusCodes.Discrepancy : SupplierInvoiceStatusCodes.Matched;
            await HydrateAsync(connection, dto, header.SupplierInvoiceId);
            PopulateComputedTotals(dto);
            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UploadAttachmentAsync(Guid supplierInvoiceToken, Stream fileStream, string fileExtension, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var invoice = await connection.QueryFirstOrDefaultAsync<SupplierInvoice>(
            "sp_SupplierInvoice_GetByToken", new { SupplierInvoiceToken = supplierInvoiceToken }, commandType: CommandType.StoredProcedure);
        if (invoice is null)
            return false;

        if (!await CanManageSupplierInvoicesAsync(connection, context, invoice.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceForbidden, "Cannot attach a file to a supplier invoice outside your scope.", 403);

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);

        await fileStorage.SaveAsync(supplierInvoiceToken, memoryStream.ToArray(), fileExtension, cancellationToken);

        await connection.ExecuteAsync(
            "sp_SupplierInvoice_SetAttachmentUrl",
            new { SupplierInvoiceToken = supplierInvoiceToken, AttachmentUrl = "/supplierInvoices/downloadAttachment" },
            commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<(byte[] Bytes, string Extension)?> DownloadAttachmentAsync(Guid supplierInvoiceToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var invoice = await connection.QueryFirstOrDefaultAsync<SupplierInvoice>(
            "sp_SupplierInvoice_GetByToken", new { SupplierInvoiceToken = supplierInvoiceToken }, commandType: CommandType.StoredProcedure);
        if (invoice is null)
            return null;

        if (!await CanReadOrganizationAsync(connection, context, invoice.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceForbidden, "Cannot download this attachment outside your scope.", 403);

        return await fileStorage.GetAsync(supplierInvoiceToken, cancellationToken);
    }
}
