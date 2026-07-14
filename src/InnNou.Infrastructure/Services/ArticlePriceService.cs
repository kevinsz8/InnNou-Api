using ClosedXML.Excel;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Localization;
using InnNou.Shared.Mapping;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace InnNou.Infrastructure.Services;

public class ArticlePriceService(
    IDbConnectionFactory connectionFactory,
    IMapper mapper,
    ISupplierService supplierService,
    IOrganizationService organizationService,
    ICurrencyService currencyService) : IArticlePriceService
{
    private sealed class ArticlePricePageRow : ArticlePrice { public int TotalCount { get; set; } }

    // Denormalized projection used only by ExportArticlePricesAsync — sp_ArticlePrice_GetAllForExport
    // joins in SupplierName/ArticleName/SupplierSku/OrganizationName so the export file is directly
    // human-readable, which don't fit the plain ArticlePrice DbEntity shape used elsewhere.
    private sealed class ArticlePriceExportRow
    {
        public int ArticlePriceId { get; set; }
        public Guid ArticlePriceToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string ArticleName { get; set; } = default!;
        public string? SupplierSku { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = default!;
        public int? OrganizationId { get; set; }
        public Guid? OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public DateTime EffectiveDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public int TotalCount { get; set; }
    }

    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    // Harmonized with ArticleService.CanManage: suppliers own ArticlePrices, so only the owning
    // supplier (real login or impersonated) or Admin+ may write them.
    private static bool CanManage(IRequestContext context, int supplierId) =>
        context.SupplierId.HasValue
            ? context.SupplierId.Value == supplierId
            : context.RoleLevel >= AdminRoleLevel;

    public async Task<ArticlePriceDto?> CreateAsync(ArticlePriceDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!CanManage(context, dto.SupplierId))
            throw new ApiException(ErrorCodes.ArticlePriceSupplierForbidden, "Not allowed to set prices for this supplier's articles.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticlePriceToken", Guid.NewGuid());
        p.Add("@ArticleId", dto.ArticleId);
        p.Add("@OrganizationId", dto.OrganizationId);
        p.Add("@Price", dto.Price);
        p.Add("@CurrencyCode", dto.CurrencyCode);
        p.Add("@EffectiveDate", dto.EffectiveDate);
        p.Add("@Notes", dto.Notes);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
                "sp_ArticlePrice_Create", p, commandType: CommandType.StoredProcedure);
            return row is null ? null : mapper.Map<ArticlePriceDto>(row);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new ApiException(
                ErrorCodes.ArticlePriceDuplicateEffectiveDate,
                "A price for this article/organization/currency is already effective on this date.",
                409);
        }
    }

    public async Task<ArticlePriceDto?> GetCurrentAsync(int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, DateTime asOfDate, IRequestContext context, CancellationToken cancellationToken = default)
    {
        // Everyone may read a current price; a regular org-scoped caller always resolves it for
        // their own (server-known) organization. Only the owning supplier or Admin+ may inspect
        // an explicitly requested organization's contract price.
        var effectiveOrganizationId = CanManage(context, supplierId) ? requestedOrganizationId : context.OrganizationId;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", effectiveOrganizationId);
        p.Add("@CurrencyCode", currencyCode, DbType.AnsiString, size: 10, direction: ParameterDirection.InputOutput);
        p.Add("@AsOfDate", asOfDate);

        var row = await connection.QueryFirstOrDefaultAsync<ArticlePrice>(
            "sp_ArticlePrice_GetCurrent", p, commandType: CommandType.StoredProcedure);

        var resolvedCurrencyCode = p.Get<string?>("@CurrencyCode");
        if (resolvedCurrencyCode is null)
            throw new ApiException(
                ErrorCodes.ArticlePriceCurrencyRequired,
                "A currency code could not be determined — supply one explicitly or resolve for an organization with a configured CurrencyCode.",
                400);

        return row is null ? null : mapper.Map<ArticlePriceDto>(row);
    }

    public async Task<PagedResult<ArticlePriceDto>> GetHistoryAsync(int pageNumber, int pageSize, int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        // Owning supplier/Admin see full history across every organization's contract prices;
        // everyone else is forced to their own (server-known) organization plus global prices.
        var unrestricted = CanManage(context, supplierId);
        var effectiveOrganizationId = unrestricted ? requestedOrganizationId : context.OrganizationId;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@ArticleId", articleId);
        p.Add("@OrganizationId", effectiveOrganizationId);
        p.Add("@CurrencyCode", currencyCode);
        p.Add("@UnrestrictedOrganizationAccess", unrestricted);
        var rows = (await connection.QueryAsync<ArticlePricePageRow>(
            "sp_ArticlePrice_GetHistory", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<ArticlePriceDto>
        {
            Items = mapper.MapList<ArticlePriceDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<BulkImportArticlePriceResultDto> BulkImportArticlePricesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default)
    {
        // Same gate shape as ArticleService.BulkImportArticlesAsync: the owning supplier may
        // bulk-price their own catalog, Admin+ may bulk-price any supplier's (resolved per row via
        // the SupplierName column) — not a flat AdminRoleLevel gate, mirroring CanManage's own
        // condition since pricing is ownership-scoped at the single-row level already.
        if (!context.SupplierId.HasValue && context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ArticlePriceBulkImportForbidden, "Only the owning supplier or Admins/SuperAdmins can bulk-import article prices.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.ArticlePriceBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.ArticlePriceBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportArticlePriceResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            await using var connection = connectionFactory.CreateConnection();

            var supplierCache = new Dictionary<string, Supplier?>(StringComparer.OrdinalIgnoreCase);
            var articleCache = new Dictionary<string, Article?>(StringComparer.OrdinalIgnoreCase);
            var organizationCache = new Dictionary<string, Organization?>(StringComparer.OrdinalIgnoreCase);

            // IMPORTANT: rows processed strictly sequentially, same convention as every other bulk
            // import in this codebase. ArticlePrices is insert-only (CLAUDE.md) — reimporting a row
            // for the same Article+Organization+Currency+EffectiveDate is rejected as a duplicate
            // (ArticlePriceDuplicateEffectiveDate, via the same unique-index-violation translation
            // CreateAsync already does for the single-row endpoint), never silently overwritten.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();

                var supplierName = row.Cell(1).GetString().Trim();
                var supplierSku = row.Cell(2).GetString().Trim();
                var organizationName = row.Cell(3).GetString().Trim();
                var priceText = row.Cell(4).GetString().Trim();
                var currencyCodeText = row.Cell(5).GetString().Trim();
                var effectiveDateCell = row.Cell(6);
                var notes = row.Cell(7).GetString().Trim();

                var rowIdentifier = string.IsNullOrWhiteSpace(supplierSku) ? null : supplierSku;

                // --- Resolve supplier (same pattern as ArticleService.BulkImportArticlesAsync) ---
                int supplierId;
                if (context.SupplierId.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(supplierName))
                    {
                        var ownKey = supplierName.ToUpperInvariant();
                        if (!supplierCache.TryGetValue(ownKey, out var maybeOwnSupplier))
                        {
                            maybeOwnSupplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
                                "sp_Supplier_GetByNormalizedName", new { NormalizedName = ownKey }, commandType: CommandType.StoredProcedure);
                            supplierCache[ownKey] = maybeOwnSupplier;
                        }
                        if (maybeOwnSupplier is null || maybeOwnSupplier.SupplierId != context.SupplierId.Value)
                        {
                            result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceSupplierForbidden, Description = "SupplierName does not match your own supplier account." });
                            continue;
                        }
                    }
                    supplierId = context.SupplierId.Value;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(supplierName))
                    {
                        result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceBulkImportRowInvalid, Description = "SupplierName is required." });
                        continue;
                    }

                    var supplierKey = supplierName.ToUpperInvariant();
                    if (!supplierCache.TryGetValue(supplierKey, out var supplier))
                    {
                        supplier = await connection.QueryFirstOrDefaultAsync<Supplier>(
                            "sp_Supplier_GetByNormalizedName", new { NormalizedName = supplierKey }, commandType: CommandType.StoredProcedure);
                        supplierCache[supplierKey] = supplier;
                    }
                    if (supplier is null)
                    {
                        result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.SupplierNotFound, Description = $"Supplier '{supplierName}' was not found." });
                        continue;
                    }
                    supplierId = supplier.SupplierId;
                }

                // --- Resolve article by (SupplierId, SupplierSku) — the identifying key chosen
                // for this template, since it's already the natural per-supplier business key
                // (sp_Article_ExistsBySupplierSku) rather than an opaque ArticleToken. ---
                if (string.IsNullOrWhiteSpace(supplierSku))
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceBulkImportRowInvalid, Description = "SupplierSku is required." });
                    continue;
                }

                var articleKey = $"{supplierId}:{supplierSku.ToUpperInvariant()}";
                if (!articleCache.TryGetValue(articleKey, out var article))
                {
                    article = await connection.QueryFirstOrDefaultAsync<Article>(
                        "sp_Article_GetBySupplierSku", new { SupplierId = supplierId, SupplierSku = supplierSku }, commandType: CommandType.StoredProcedure);
                    articleCache[articleKey] = article;
                }
                if (article is null)
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = $"No article with SupplierSku '{supplierSku}' was found for this supplier." });
                    continue;
                }
                if (article.ReplacedByArticleId.HasValue)
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceArticleReplaced, Description = "This article has been superseded — price the replacement article instead." });
                    continue;
                }

                // --- Resolve organization (blank = global price) ---
                int? organizationId = null;
                if (!string.IsNullOrWhiteSpace(organizationName))
                {
                    var organizationKey = organizationName.ToUpperInvariant();
                    if (!organizationCache.TryGetValue(organizationKey, out var organization))
                    {
                        organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                            "sp_Organization_GetByNormalizedName", new { NormalizedName = organizationKey }, commandType: CommandType.StoredProcedure);
                        organizationCache[organizationKey] = organization;
                    }
                    if (organization is null)
                    {
                        result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.OrganizationNotFound, Description = $"Organization '{organizationName}' was not found." });
                        continue;
                    }
                    organizationId = organization.OrganizationId;
                }

                if (string.IsNullOrWhiteSpace(currencyCodeText))
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceBulkImportRowInvalid, Description = "CurrencyCode is required." });
                    continue;
                }
                var currencyCode = currencyCodeText.ToUpperInvariant();
                if (!await currencyService.ExistsActiveByCodeAsync(currencyCode, cancellationToken))
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceInvalidCurrency, Description = $"Currency '{currencyCodeText}' is not a recognized, active currency." });
                    continue;
                }

                if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceInvalidAmount, Description = "Price is required and must be greater than zero." });
                    continue;
                }

                DateTime effectiveDate;
                try
                {
                    effectiveDate = effectiveDateCell.GetDateTime().Date;
                }
                catch
                {
                    if (!DateTime.TryParse(effectiveDateCell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out effectiveDate))
                    {
                        result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceBulkImportRowInvalid, Description = "EffectiveDate is required and must be a valid date." });
                        continue;
                    }
                    effectiveDate = effectiveDate.Date;
                }

                try
                {
                    var dto = new ArticlePriceDto
                    {
                        ArticleId = article.ArticleId,
                        SupplierId = article.SupplierId,
                        OrganizationId = organizationId,
                        Price = price,
                        CurrencyCode = currencyCode,
                        EffectiveDate = effectiveDate,
                        Notes = NullIfEmpty(notes)
                    };

                    var created = await CreateAsync(dto, context, cancellationToken);
                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceCreateFailed, Description = "Article price could not be created." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportArticlePriceRowErrorDto { RowNumber = rowNumber, SupplierSku = rowIdentifier, Code = ErrorCodes.ArticlePriceBulkImportRowFailed, Description = "An unexpected error occurred while processing this row." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportArticlePricesAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!context.SupplierId.HasValue && context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ArticlePriceBulkImportForbidden, "Only the owning supplier or Admins/SuperAdmins can export article prices.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", 1);
        p.Add("@PageSize", MaxExportRows);
        p.Add("@SupplierId", context.SupplierId);
        var rows = (await connection.QueryAsync<ArticlePriceExportRow>(
            "sp_ArticlePrice_GetAllForExport", p, commandType: CommandType.StoredProcedure)).ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("ArticlePrices");

        string[] headers = ["SupplierName", "SupplierSku", "ArticleName", "OrganizationName", "Price", "CurrencyCode", "EffectiveDate", "Notes", "CreatedUtc"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var price in rows)
        {
            worksheet.Cell(r, 1).Value = price.SupplierName;
            worksheet.Cell(r, 2).Value = price.SupplierSku;
            worksheet.Cell(r, 3).Value = price.ArticleName;
            worksheet.Cell(r, 4).Value = price.OrganizationName;
            worksheet.Cell(r, 5).Value = price.Price;
            worksheet.Cell(r, 6).Value = price.CurrencyCode;
            worksheet.Cell(r, 7).Value = price.EffectiveDate;
            worksheet.Cell(r, 8).Value = price.Notes;
            worksheet.Cell(r, 9).Value = price.CreatedUtc;
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"article_prices_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateArticlePriceImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!context.SupplierId.HasValue && context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ArticlePriceBulkImportForbidden, "Only the owning supplier or Admins/SuperAdmins can download the import template.", 403);

        using var workbook = new XLWorkbook();

        var pricesSheet = workbook.Worksheets.Add("ArticlePrices");
        string[] headers = ["SupplierName", "SupplierSku", "OrganizationName", "Price", "CurrencyCode", "EffectiveDate", "Notes"];
        for (var i = 0; i < headers.Length; i++)
            pricesSheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        pricesSheet.Row(1).Style.Font.Bold = true;

        // Suppliers reference sheet + dropdown — Admin+ only, same reasoning as
        // ArticleService.GenerateArticleImportTemplateAsync.
        if (!context.SupplierId.HasValue)
        {
            var suppliers = await supplierService.GetSuppliersAsync(1, MaxExportRows, null, null, false, context, cancellationToken);
            var suppliersSheet = workbook.Worksheets.Add("Suppliers");
            suppliersSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Name", language);
            suppliersSheet.Row(1).Style.Font.Bold = true;
            var supplierRow = 2;
            foreach (var supplier in suppliers.Items.OrderBy(s => s.Name))
                suppliersSheet.Cell(supplierRow++, 1).Value = supplier.Name;
            suppliersSheet.Columns().AdjustToContents();

            if (suppliers.Items.Count > 0)
            {
                var supplierNamedRange = workbook.DefinedNames.Add("ArticlePriceImportSupplierNames", suppliersSheet.Range(2, 1, supplierRow - 1, 1));
                pricesSheet.Range(2, 1, MaxBulkImportRows + 1, 1).CreateDataValidation().List(supplierNamedRange.Name, true);
            }
        }

        // Organizations reference sheet + dropdown for the optional OrganizationName column
        // (blank = global list price) — scoped to the caller's own hierarchy like every other
        // Organization reference sheet in this codebase.
        var organizations = await organizationService.GetOrganizationsAsync(1, MaxExportRows, null, null, false, context, cancellationToken);
        var organizationsSheet = workbook.Worksheets.Add("Organizations");
        organizationsSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Name", language);
        organizationsSheet.Row(1).Style.Font.Bold = true;
        var organizationRow = 2;
        foreach (var organization in organizations.Items.OrderBy(o => o.Name))
            organizationsSheet.Cell(organizationRow++, 1).Value = organization.Name;
        organizationsSheet.Columns().AdjustToContents();

        if (organizations.Items.Count > 0)
        {
            var organizationNamedRange = workbook.DefinedNames.Add("ArticlePriceImportOrganizationNames", organizationsSheet.Range(2, 1, organizationRow - 1, 1));
            pricesSheet.Range(2, 3, MaxBulkImportRows + 1, 3).CreateDataValidation().List(organizationNamedRange.Name, true);
        }

        // Currencies reference sheet + dropdown — short, mostly-static list (~154 active ISO 4217
        // codes), cheap to include and removes free-text typos as a row-error source.
        var currencies = await currencyService.GetAllAsync(false, cancellationToken);
        var currenciesSheet = workbook.Worksheets.Add("Currencies");
        currenciesSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Code", language);
        currenciesSheet.Row(1).Style.Font.Bold = true;
        var currencyRow = 2;
        foreach (var currency in currencies.OrderBy(c => c.Code))
            currenciesSheet.Cell(currencyRow++, 1).Value = currency.Code;
        currenciesSheet.Columns().AdjustToContents();

        if (currencies.Count > 0)
        {
            var currencyNamedRange = workbook.DefinedNames.Add("ArticlePriceImportCurrencyCodes", currenciesSheet.Range(2, 1, currencyRow - 1, 1));
            pricesSheet.Range(2, 5, MaxBulkImportRows + 1, 5).CreateDataValidation().List(currencyNamedRange.Name, true);
        }

        pricesSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), "article_prices_import_template.xlsx");
    }
}
