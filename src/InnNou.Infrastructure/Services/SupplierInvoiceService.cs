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
    IWarehouseService warehouseService,
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

    public async Task<List<PurchaseOrderDto>> GetEligiblePurchaseOrdersAsync(Guid organizationToken, Guid supplierToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);
        if (organization is null)
            return [];

        if (!await CanManageSupplierInvoicesAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierInvoiceForbidden, "Cannot browse purchase orders outside your scope.", 403);

        var supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken", new { SupplierToken = supplierToken }, commandType: CommandType.StoredProcedure);
        if (supplier is null)
            return [];

        var rows = await connection.QueryAsync<PurchaseOrder>(
            "sp_PurchaseOrder_GetEligibleForInvoicing", new { organization.OrganizationId, supplier.SupplierId }, commandType: CommandType.StoredProcedure);

        return mapper.MapList<PurchaseOrderDto>(rows.ToList());
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

    // One resolved, invoiceable line — the effective PurchaseOrderLine values (what was
    // ordered/received) plus the caller-supplied (possibly corrected) invoiced values.
    private sealed record ResolvedLine(
        int PurchaseOrderLineId,
        int PurchaseOrderId,
        int ArticleId,
        int WarehouseId,
        Guid WarehouseToken,
        string CurrencyCode,
        decimal ExpectedQuantity,
        decimal ExpectedUnitPrice,
        decimal QuantityInvoiced,
        decimal UnitPriceInvoiced);

    public async Task<SupplierInvoiceDto?> CreateAsync(Guid organizationToken, Guid supplierToken, string supplierInvoiceNumber, DateTime invoiceDate, string? notes, List<Guid> purchaseOrderTokens, List<CreateSupplierInvoiceLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken)
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

        if (purchaseOrderTokens.Count == 0)
            throw new ApiException(ErrorCodes.SupplierInvoiceEmpty, "At least one purchase order must be selected.", 400);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.SupplierInvoiceEmpty, "At least one line must be invoiced.", 400);

        // A PO is invoiced entirely in one shot — collect every non-cancelled line of every
        // selected PO as the set the submitted lines must match exactly (see the count check
        // below). purchaseOrderIdByToken also lets the transaction below set each PO's status
        // without re-fetching it.
        var expectedLines = new Dictionary<Guid, ResolvedLine>();
        var purchaseOrderIdByToken = new Dictionary<Guid, int>();

        foreach (var purchaseOrderToken in purchaseOrderTokens)
        {
            var purchaseOrder = await purchaseOrderService.GetByTokenAsync(purchaseOrderToken, context, cancellationToken);
            if (purchaseOrder is null)
                throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderNotFound, $"Purchase order '{purchaseOrderToken}' not found.", 404);

            if (purchaseOrder.OrganizationId != organization.OrganizationId)
                throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderNotFound, $"Purchase order '{purchaseOrder.PurchaseOrderNumber}' does not belong to this organization.", 404);

            if (purchaseOrder.SupplierId != supplier.SupplierId)
                throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderDifferentSupplier, $"Purchase order '{purchaseOrder.PurchaseOrderNumber}' belongs to a different supplier.", 400);

            if (purchaseOrder.Status != PurchaseOrderStatusCodes.Received)
                throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderNotReceived, $"Purchase order '{purchaseOrder.PurchaseOrderNumber}' must be fully received before it can be invoiced.", 409);

            purchaseOrderIdByToken[purchaseOrderToken] = purchaseOrder.PurchaseOrderId;

            foreach (var line in purchaseOrder.Lines.Where(l => !l.IsCancelled))
            {
                expectedLines[line.PurchaseOrderLineToken] = new ResolvedLine(
                    PurchaseOrderLineId: line.PurchaseOrderLineId,
                    PurchaseOrderId: purchaseOrder.PurchaseOrderId,
                    ArticleId: line.ArticleId,
                    WarehouseId: purchaseOrder.WarehouseId,
                    WarehouseToken: purchaseOrder.WarehouseToken,
                    CurrencyCode: line.CurrencyCode,
                    ExpectedQuantity: line.Quantity,
                    ExpectedUnitPrice: line.UnitPrice,
                    QuantityInvoiced: 0,
                    UnitPriceInvoiced: 0);
            }
        }

        var submittedTokens = new HashSet<Guid>();
        var resolvedLines = new List<ResolvedLine>();

        foreach (var input in lines)
        {
            if (!expectedLines.TryGetValue(input.PurchaseOrderLineToken, out var expected))
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, $"Line '{input.PurchaseOrderLineToken}' does not belong to any of the selected purchase orders.", 400);

            if (!submittedTokens.Add(input.PurchaseOrderLineToken))
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, $"Line '{input.PurchaseOrderLineToken}' was submitted more than once.", 400);

            if (input.QuantityInvoiced <= 0)
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, "Invoiced quantity must be greater than zero.", 400);

            if (input.UnitPriceInvoiced < 0)
                throw new ApiException(ErrorCodes.SupplierInvoiceLineInvalid, "Invoiced unit price cannot be negative.", 400);

            resolvedLines.Add(expected with { QuantityInvoiced = input.QuantityInvoiced, UnitPriceInvoiced = input.UnitPriceInvoiced });
        }

        if (submittedTokens.Count != expectedLines.Count)
            throw new ApiException(ErrorCodes.SupplierInvoiceLineIncomplete, "Every line of every selected purchase order must be invoiced — a purchase order is always invoiced in full.", 400);

        // Tolerance — a hard requirement, unlike tax below (matching cannot be computed without it).
        var tolerance = await connection.QueryFirstOrDefaultAsync<SupplierInvoiceMatchTolerance>(
            "sp_SupplierInvoiceMatchTolerance_GetEffective", new { organization.OrganizationId }, commandType: CommandType.StoredProcedure);
        if (tolerance is null)
            throw new ApiException(ErrorCodes.SupplierInvoiceToleranceNotConfigured, "No invoice-matching tolerance is configured for this organization or any of its ancestors — configure one before creating a supplier invoice.", 400);

        // Tax — informational only (matching tolerance is evaluated on the net/taxable amount,
        // never the tax-inclusive total, same reasoning SAP/Odoo use: VAT is a recoverable
        // pass-through liability, not a cost to reconcile). A missing tax configuration degrades
        // gracefully to null tax fields here rather than blocking invoice creation — unlike
        // GoodsReceipt's own hard validation, since Fase A already enforced tax completeness at
        // receipt time, a gap here would be a pre-existing data issue, not something this step
        // should police.
        var distinctArticleIds = resolvedLines.Select(l => l.ArticleId).Distinct().ToList();
        var effectiveCategories = (await connection.QueryAsync<ArticleEffectiveTaxCategory>(
            "sp_Article_GetEffectiveTaxCategoryByIds",
            new { ArticleIds = string.Join(",", distinctArticleIds) },
            commandType: CommandType.StoredProcedure)).ToDictionary(a => a.ArticleId);

        var ratesByWarehouseId = new Dictionary<int, Dictionary<int, TaxRate>>();
        foreach (var line in resolvedLines.DistinctBy(l => l.WarehouseId))
        {
            var warehouse = await warehouseService.GetByTokenAsync(line.WarehouseToken, context, cancellationToken);
            if (warehouse?.TaxJurisdictionId is null)
            {
                ratesByWarehouseId[line.WarehouseId] = [];
                continue;
            }

            var rates = await connection.QueryAsync<TaxRate>(
                "sp_TaxRate_GetByJurisdictionId", new { TaxJurisdictionId = warehouse.TaxJurisdictionId }, commandType: CommandType.StoredProcedure);
            ratesByWarehouseId[line.WarehouseId] = rates.ToDictionary(r => r.TaxCategoryId);
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

            var anyDiscrepancy = false;

            foreach (var line in resolvedLines)
            {
                var taxableAmount = line.QuantityInvoiced * line.UnitPriceInvoiced;
                var expectedNetAmount = line.ExpectedQuantity * line.ExpectedUnitPrice;
                var amountDiff = Math.Abs(taxableAmount - expectedNetAmount);
                var percentDiff = expectedNetAmount == 0
                    ? (taxableAmount == 0 ? 0 : 100)
                    : amountDiff / expectedNetAmount * 100;
                var isWithinTolerance = percentDiff <= tolerance.TolerancePercent && amountDiff <= tolerance.ToleranceAmount;
                if (!isWithinTolerance)
                    anyDiscrepancy = true;

                int? taxCategoryId = null;
                decimal? taxRatePercent = null;
                decimal? taxAmount = null;

                if (effectiveCategories.TryGetValue(line.ArticleId, out var effective) && effective.TaxCategoryId.HasValue
                    && ratesByWarehouseId.TryGetValue(line.WarehouseId, out var rates) && rates.TryGetValue(effective.TaxCategoryId.Value, out var rate))
                {
                    taxCategoryId = effective.TaxCategoryId.Value;
                    taxRatePercent = rate.RatePercent;
                    taxAmount = Math.Round(taxableAmount * rate.RatePercent / 100m, 4);
                }

                var totalAmount = taxableAmount + (taxAmount ?? 0);

                var lineParams = new DynamicParameters();
                lineParams.Add("@SupplierInvoiceLineToken", Guid.NewGuid());
                lineParams.Add("@SupplierInvoiceId", header.SupplierInvoiceId);
                lineParams.Add("@PurchaseOrderLineId", line.PurchaseOrderLineId);
                lineParams.Add("@ArticleId", line.ArticleId);
                lineParams.Add("@QuantityInvoiced", line.QuantityInvoiced);
                lineParams.Add("@UnitPriceInvoiced", line.UnitPriceInvoiced);
                lineParams.Add("@CurrencyCode", line.CurrencyCode);
                lineParams.Add("@TaxCategoryId", taxCategoryId);
                lineParams.Add("@TaxRatePercent", taxRatePercent);
                lineParams.Add("@TaxableAmount", taxableAmount);
                lineParams.Add("@TaxAmount", taxAmount);
                lineParams.Add("@TotalAmount", totalAmount);
                lineParams.Add("@IsWithinTolerance", isWithinTolerance);
                lineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_SupplierInvoiceLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            foreach (var (purchaseOrderToken, purchaseOrderId) in purchaseOrderIdByToken)
            {
                try
                {
                    await connection.ExecuteAsync(
                        "sp_SupplierInvoicePurchaseOrder_Create",
                        new { header.SupplierInvoiceId, PurchaseOrderId = purchaseOrderId },
                        transaction, commandType: CommandType.StoredProcedure);
                }
                catch (SqlException ex) when (ex.Number is 2601 or 2627)
                {
                    throw new ApiException(ErrorCodes.SupplierInvoicePurchaseOrderAlreadyInvoiced, "One of the selected purchase orders was already invoiced by another request.", 409);
                }

                await connection.ExecuteAsync(
                    "sp_PurchaseOrder_SetStatus",
                    new { PurchaseOrderToken = purchaseOrderToken, Status = PurchaseOrderStatusCodes.Invoiced },
                    transaction, commandType: CommandType.StoredProcedure);
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
