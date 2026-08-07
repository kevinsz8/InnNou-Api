using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

// Facturación Fase C ("Notas de crédito") — see .claude/SupplierCreditNoteModule.md for the full
// design writeup (RD 1619/2012, SAP MIRO Credit Memo, Odoo vendor credit notes research) and
// migrations/20260807_SupplierCreditNotes_Create.sql for the schema-level reasoning.
public class SupplierCreditNoteService(
    IDbConnectionFactory connectionFactory,
    IMapper mapper) : ISupplierCreditNoteService
{
    private sealed class SupplierCreditNotePageRow : SupplierCreditNote { public int TotalCount { get; set; } }

    // Same tier as SupplierInvoice — a real fiscal document correcting money owed, one level
    // above SupplierReturn's own Staff+ (segregation-of-duties precedent, confirmed with the
    // user against the same AP research SupplierInvoiceModule.md already documents).
    private const int AdminRoleLevel = 80;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    private static async Task<bool> CanManageSupplierCreditNotesAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
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

    private async Task HydrateAsync(IDbConnection connection, SupplierCreditNoteDto dto, int supplierCreditNoteId)
    {
        dto.Lines = mapper.MapList<SupplierCreditNoteLineDto>(
            await connection.QueryAsync<SupplierCreditNoteLine>(
                "sp_SupplierCreditNoteLine_GetBySupplierCreditNoteId", new { SupplierCreditNoteId = supplierCreditNoteId }, commandType: CommandType.StoredProcedure));

        dto.TaxBreakdown = mapper.MapList<SupplierCreditNoteTaxBreakdownDto>(
            await connection.QueryAsync<SupplierCreditNoteTaxBreakdown>(
                "sp_SupplierCreditNoteTaxBreakdown_GetBySupplierCreditNoteId", new { SupplierCreditNoteId = supplierCreditNoteId }, commandType: CommandType.StoredProcedure));

        dto.CorrectedInvoices = mapper.MapList<SupplierCreditNoteInvoiceRefDto>(
            await connection.QueryAsync<SupplierCreditNoteInvoiceRef>(
                "sp_SupplierCreditNoteInvoice_GetBySupplierCreditNoteId", new { SupplierCreditNoteId = supplierCreditNoteId }, commandType: CommandType.StoredProcedure));

        // Computed from the lines just fetched, not a separate SQL aggregate — GetPaged's own SP
        // still does its own CROSS APPLY SUM since it never fetches per-row Lines.
        dto.LineCount = dto.Lines.Count;
        dto.TotalAmount = dto.Lines.Sum(l => l.TotalAmount);
    }

    public async Task<SupplierCreditNoteDto?> CreateAsync(Guid supplierReturnToken, string creditNoteNumber, DateTime creditNoteDate, string reason, string? notes, List<CreateSupplierCreditNoteLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(creditNoteNumber))
            throw new ApiException(ErrorCodes.InvalidRequest, "A credit note number is required.", 400);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ApiException(ErrorCodes.InvalidRequest, "A reason is required — RD 1619/2012 requires every factura rectificativa to state its cause.", 400);

        await using var connection = connectionFactory.CreateConnection();

        var supplierReturn = await connection.QueryFirstOrDefaultAsync<SupplierReturn>(
            "sp_SupplierReturn_GetByToken", new { SupplierReturnToken = supplierReturnToken }, commandType: CommandType.StoredProcedure);
        if (supplierReturn is null)
            throw new ApiException(ErrorCodes.SupplierCreditNoteReturnNotFound, "Supplier return not found.", 404);

        if (!await CanManageSupplierCreditNotesAsync(connection, context, supplierReturn.OrganizationId))
            throw new ApiException(ErrorCodes.SupplierCreditNoteForbidden, "Cannot create a credit note outside your scope.", 403);

        if (supplierReturn.Status != SupplierReturnStatus.Closed || supplierReturn.ResolutionType != SupplierReturnResolutionType.Credited)
            throw new ApiException(ErrorCodes.SupplierCreditNoteReturnNotCredited, "A credit note can only be registered for a supplier return closed with resolution CREDITED.", 409);

        var existingNote = await connection.QueryFirstOrDefaultAsync<SupplierCreditNote>(
            "sp_SupplierCreditNote_GetBySupplierReturnId", new { supplierReturn.SupplierReturnId }, commandType: CommandType.StoredProcedure);
        if (existingNote is not null)
            throw new ApiException(ErrorCodes.SupplierCreditNoteAlreadyExists, "This supplier return already has a credit note registered.", 409);

        if (lines.Count == 0)
            throw new ApiException(ErrorCodes.SupplierCreditNoteEmpty, "At least one line must be credited.", 400);

        // Every line of the return must be credited in one shot — no partial/repeatable credit
        // notes in V1 (a deliberate scope decision, see .claude/SupplierCreditNoteModule.md).
        var returnLines = (await connection.QueryAsync<SupplierReturnLine>(
            "sp_SupplierReturnLine_GetBySupplierReturnId", new { supplierReturn.SupplierReturnId }, commandType: CommandType.StoredProcedure))
            .ToDictionary(l => l.SupplierReturnLineToken);

        if (lines.Select(l => l.SupplierReturnLineToken).Distinct().Count() != returnLines.Count
            || lines.Any(l => !returnLines.ContainsKey(l.SupplierReturnLineToken)))
            throw new ApiException(ErrorCodes.SupplierCreditNoteLineNotEligible, "The submitted lines must match this supplier return's own lines exactly — one credit note covers a whole return.", 400);

        string? fallbackCurrencyCode = null;
        var resolvedLines = new List<(SupplierReturnLine ReturnLine, decimal UnitPrice, string CurrencyCode, bool WasManuallyEntered)>();
        foreach (var input in lines)
        {
            var returnLine = returnLines[input.SupplierReturnLineToken];

            var unitPrice = input.UnitPrice ?? returnLine.UnitPrice
                ?? throw new ApiException(ErrorCodes.SupplierCreditNoteUnitPriceRequired, $"Article '{returnLine.ArticleName}' has no price on file (received before unit-price freezing existed) — enter it manually.", 400);

            if (unitPrice <= 0)
                throw new ApiException(ErrorCodes.SupplierCreditNoteUnitPriceRequired, $"Article '{returnLine.ArticleName}' needs a positive unit price.", 400);

            var currencyCode = returnLine.CurrencyCode;
            if (currencyCode is null)
            {
                if (fallbackCurrencyCode is null)
                {
                    var currencyParams = new DynamicParameters();
                    currencyParams.Add("@OrganizationId", supplierReturn.OrganizationId);
                    currencyParams.Add("@CurrencyCode", null, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
                    await connection.ExecuteAsync("sp_Organization_ResolveCurrencyCode", currencyParams, commandType: CommandType.StoredProcedure);
                    fallbackCurrencyCode = currencyParams.Get<string?>("@CurrencyCode")
                        ?? throw new ApiException(ErrorCodes.ArticlePriceCurrencyRequired, "No currency could be resolved for this organization — configure one before registering a credit note for a pre-2026-08-07 return line.", 400);
                }
                currencyCode = fallbackCurrencyCode;
            }

            resolvedLines.Add((returnLine, unitPrice, currencyCode, input.UnitPrice.HasValue));
        }

        var actor = context.ActorUserToken.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var headerParams = new DynamicParameters();
            headerParams.Add("@SupplierCreditNoteToken", Guid.NewGuid());
            headerParams.Add("@SupplierReturnId", supplierReturn.SupplierReturnId);
            headerParams.Add("@OrganizationId", supplierReturn.OrganizationId);
            headerParams.Add("@SupplierId", supplierReturn.SupplierId);
            headerParams.Add("@CreditNoteNumber", creditNoteNumber.Trim());
            headerParams.Add("@CreditNoteDate", creditNoteDate.Date);
            headerParams.Add("@Reason", reason.Trim());
            headerParams.Add("@Notes", notes);
            headerParams.Add("@CreatedBy", actor);

            var header = await connection.QueryFirstOrDefaultAsync<SupplierCreditNote>(
                "sp_SupplierCreditNote_Create", headerParams, transaction, commandType: CommandType.StoredProcedure)
                ?? throw new InvalidOperationException("sp_SupplierCreditNote_Create returned no row for an already-validated SupplierReturn.");

            foreach (var (returnLine, unitPrice, currencyCode, wasManuallyEntered) in resolvedLines)
            {
                var taxableAmount = returnLine.QuantityRejected * unitPrice;
                var taxAmount = returnLine.TaxRatePercent.HasValue
                    ? Math.Round(taxableAmount * returnLine.TaxRatePercent.Value / 100m, 8)
                    : 0m;

                var lineParams = new DynamicParameters();
                lineParams.Add("@SupplierCreditNoteLineToken", Guid.NewGuid());
                lineParams.Add("@SupplierCreditNoteId", header.SupplierCreditNoteId);
                lineParams.Add("@SupplierReturnLineId", returnLine.SupplierReturnLineId);
                lineParams.Add("@ArticleId", returnLine.ArticleId);
                lineParams.Add("@QuantityCredited", returnLine.QuantityRejected);
                lineParams.Add("@UnitPrice", unitPrice);
                lineParams.Add("@CurrencyCode", currencyCode);
                lineParams.Add("@TaxCategoryId", returnLine.TaxCategoryId);
                lineParams.Add("@TaxRatePercent", returnLine.TaxRatePercent);
                lineParams.Add("@TaxableAmount", taxableAmount);
                lineParams.Add("@TaxAmount", taxAmount);
                lineParams.Add("@TotalAmount", taxableAmount + taxAmount);
                lineParams.Add("@WasManuallyEntered", wasManuallyEntered);
                lineParams.Add("@CreatedBy", actor);

                await connection.ExecuteAsync("sp_SupplierCreditNoteLine_Create", lineParams, transaction, commandType: CommandType.StoredProcedure);
            }

            // Auto-detect which SupplierInvoice(s), if any, already cover the GoodsReceipts these
            // lines came from — never user-picked, see the migration's own header comment.
            var goodsReceiptIds = resolvedLines.Select(r => r.ReturnLine.GoodsReceiptId).Distinct().ToList();
            var invoiceIds = await connection.QueryAsync<int>(
                "sp_SupplierInvoiceGoodsReceipt_GetInvoiceIdsByGoodsReceiptIds",
                new { GoodsReceiptIds = string.Join(',', goodsReceiptIds) }, transaction, commandType: CommandType.StoredProcedure);

            foreach (var invoiceId in invoiceIds)
            {
                await connection.ExecuteAsync(
                    "sp_SupplierCreditNoteInvoice_Create",
                    new { header.SupplierCreditNoteId, SupplierInvoiceId = invoiceId, CreatedBy = actor },
                    transaction, commandType: CommandType.StoredProcedure);
            }

            // Purely computed from the lines just inserted — no separate externally-authored
            // "stated" number to reconcile against (unlike SupplierInvoiceTaxBreakdown), see the
            // migration's own header comment.
            foreach (var group in resolvedLines.GroupBy(r => (r.ReturnLine.TaxRatePercent ?? 0m, r.CurrencyCode)))
            {
                var groupTaxableAmount = group.Sum(r => r.ReturnLine.QuantityRejected * r.UnitPrice);
                var groupTaxAmount = Math.Round(groupTaxableAmount * group.Key.Item1 / 100m, 8);

                await connection.ExecuteAsync(
                    "sp_SupplierCreditNoteTaxBreakdown_Create",
                    new
                    {
                        SupplierCreditNoteTaxBreakdownToken = Guid.NewGuid(),
                        header.SupplierCreditNoteId,
                        TaxRatePercent = group.Key.Item1,
                        TaxableAmount = groupTaxableAmount,
                        TaxAmount = groupTaxAmount,
                        CurrencyCode = group.Key.Item2,
                        CreatedBy = actor
                    },
                    transaction, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            var dto = mapper.Map<SupplierCreditNoteDto>(header);
            await HydrateAsync(connection, dto, header.SupplierCreditNoteId);
            return dto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SupplierCreditNoteDto?> GetByTokenAsync(Guid supplierCreditNoteToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<SupplierCreditNote>(
            "sp_SupplierCreditNote_GetByToken", new { SupplierCreditNoteToken = supplierCreditNoteToken }, commandType: CommandType.StoredProcedure);

        if (header is null || !await CanReadOrganizationAsync(connection, context, header.OrganizationId))
            return null;

        var dto = mapper.Map<SupplierCreditNoteDto>(header);
        await HydrateAsync(connection, dto, header.SupplierCreditNoteId);
        return dto;
    }

    public async Task<SupplierCreditNoteDto?> GetBySupplierReturnTokenAsync(Guid supplierReturnToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var supplierReturn = await connection.QueryFirstOrDefaultAsync<SupplierReturn>(
            "sp_SupplierReturn_GetByToken", new { SupplierReturnToken = supplierReturnToken }, commandType: CommandType.StoredProcedure);
        if (supplierReturn is null || !await CanReadOrganizationAsync(connection, context, supplierReturn.OrganizationId))
            return null;

        var header = await connection.QueryFirstOrDefaultAsync<SupplierCreditNote>(
            "sp_SupplierCreditNote_GetBySupplierReturnId", new { supplierReturn.SupplierReturnId }, commandType: CommandType.StoredProcedure);
        if (header is null)
            return null;

        var dto = mapper.Map<SupplierCreditNoteDto>(header);
        await HydrateAsync(connection, dto, header.SupplierCreditNoteId);
        return dto;
    }

    public async Task<PagedResult<SupplierCreditNoteDto>> GetPagedAsync(Guid? organizationToken, Guid? supplierToken, DateTime? fromDate, DateTime? toDate, string? purchaseOrderNumber, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        // Same shape as SupplierInvoiceService.GetPagedAsync — an explicit organizationToken
        // always wins; omitting it falls back to a whole-hierarchy search, but that fallback is
        // deliberately restricted to non-ASSOCIATE callers (SuperAdmin/Admin/Super Asociado): an
        // ASSOCIATE (single-property) caller must always pick their own organization explicitly.
        // Kept in sync with SupplierInvoiceService's own branch for consistency between these two
        // near-identical list endpoints.
        int? rootOrganizationId;

        if (organizationToken.HasValue)
        {
            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken", new { OrganizationToken = organizationToken.Value }, commandType: CommandType.StoredProcedure);

            if (organization is null)
                return new PagedResult<SupplierCreditNoteDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            if (!await CanReadOrganizationAsync(connection, context, organization.OrganizationId))
                throw new ApiException(ErrorCodes.SupplierCreditNoteForbidden, "Cannot view supplier credit notes outside your scope.", 403);

            rootOrganizationId = organization.OrganizationId;
        }
        else if (context.RoleLevel >= SuperAdminRoleLevel)
        {
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

            if (supplier is null)
                return new PagedResult<SupplierCreditNoteDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

            supplierId = supplier.SupplierId;
        }

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", rootOrganizationId);
        p.Add("@SupplierId", supplierId);
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@PurchaseOrderNumber", string.IsNullOrWhiteSpace(purchaseOrderNumber) ? null : purchaseOrderNumber.Trim());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@RestrictToWarehouseId", context.WarehouseId);

        var rows = (await connection.QueryAsync<SupplierCreditNotePageRow>(
            "sp_SupplierCreditNote_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<SupplierCreditNoteDto>
        {
            Items = mapper.MapList<SupplierCreditNoteDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}
