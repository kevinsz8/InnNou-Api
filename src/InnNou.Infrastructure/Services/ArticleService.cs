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
using System.Data;
using System.Globalization;

namespace InnNou.Infrastructure.Services;

public class ArticleService(
    IDbConnectionFactory connectionFactory,
    IMapper mapper,
    ISupplierService supplierService,
    IFamilyService familyService,
    ISubFamilyService subFamilyService,
    IUnitOfMeasureService unitOfMeasureService) : IArticleService
{
    private sealed class ArticlePageRow : Article { public int TotalCount { get; set; } }

    private const int AdminRoleLevel = 80;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    // Supplier-scoped callers (real login or impersonated) may only manage their own supplier's
    // articles; below Admin and not supplier-scoped, no manage rights at all; Admin+ manages any.
    private static bool CanManage(IRequestContext context, int supplierId) =>
        context.SupplierId.HasValue
            ? context.SupplierId.Value == supplierId
            : context.RoleLevel >= AdminRoleLevel;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    public async Task<PagedResult<ArticleDto>> GetPagedAsync(int pageNumber, int pageSize, int? supplierId, int? familyId, int? subFamilyId, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        int? effectiveSupplierId;
        if (context.SupplierId.HasValue)
        {
            // Supplier-scoped caller (real login or impersonated) only ever sees its own articles.
            effectiveSupplierId = context.SupplierId.Value;
        }
        else if (!context.OrganizationId.HasValue && context.RoleLevel < AdminRoleLevel)
        {
            // Below Admin, not supplier-scoped, and not organization-scoped: no visibility into the catalog.
            return new PagedResult<ArticleDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };
        }
        else
        {
            effectiveSupplierId = supplierId;
        }

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SupplierId", effectiveSupplierId);
        p.Add("@FamilyId", familyId);
        p.Add("@SubFamilyId", subFamilyId);
        p.Add("@SearchText", searchText);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<ArticlePageRow>(
            "sp_Article_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<ArticleDto>
        {
            Items = mapper.MapList<ArticleDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<ArticleDto?> GetByTokenAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", p, commandType: CommandType.StoredProcedure);

        if (row is null)
            return null;

        if (context.SupplierId.HasValue)
        {
            if (row.SupplierId != context.SupplierId.Value)
                return null;
        }
        else if (!context.OrganizationId.HasValue && context.RoleLevel < AdminRoleLevel)
        {
            return null;
        }

        return mapper.Map<ArticleDto>(row);
    }

    public async Task<bool> ExistsBySupplierSkuAsync(int supplierId, string supplierSku, Guid? excludeToken, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SupplierId", supplierId);
        p.Add("@SupplierSku", supplierSku);
        p.Add("@ExcludeToken", excludeToken);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Article_ExistsBySupplierSku", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<ArticleDto?> CreateAsync(ArticleDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!CanManage(context, dto.SupplierId))
            throw new ApiException(ErrorCodes.ArticleSupplierForbidden, "Not allowed to create articles for this supplier.", 403);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleToken", Guid.NewGuid());
        p.Add("@SupplierId", dto.SupplierId);
        p.Add("@Name", dto.Name);
        p.Add("@Description", dto.Description);
        p.Add("@SupplierSku", dto.SupplierSku);
        p.Add("@Barcode", dto.Barcode);
        p.Add("@Brand", dto.Brand);
        p.Add("@FamilyId", dto.FamilyId);
        p.Add("@SubFamilyId", dto.SubFamilyId);
        p.Add("@PurchaseUnitId", dto.PurchaseUnitId);
        p.Add("@PurchaseQuantity", dto.PurchaseQuantity);
        p.Add("@ContentUnitId", dto.ContentUnitId);
        p.Add("@ContentQuantity", dto.ContentQuantity);
        p.Add("@BaseUnitId", dto.BaseUnitId);
        p.Add("@MinimumOrderQty", dto.MinimumOrderQty);
        p.Add("@LeadTimeDays", dto.LeadTimeDays);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDto>(row);
    }

    public async Task<ArticleDto?> EditAsync(ArticleDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!CanManage(context, dto.SupplierId))
            throw new ApiException(ErrorCodes.ArticleSupplierForbidden, "Not allowed to edit articles for this supplier.", 403);

        await using var connection = connectionFactory.CreateConnection();

        // Defense in depth: EditArticleCommandHandler and BulkImportArticlesAsync both already
        // reject a structural-field change before calling this method, but that guard lived only
        // in those two callers — a future caller that skips it would silently mutate structural
        // fields with nothing here to catch it. This re-checks independently against the current
        // DB row so the rule holds no matter which caller reaches EditAsync.
        var existing = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = dto.ArticleToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        var isStructuralChange =
            dto.PurchaseUnitId != existing.PurchaseUnitId ||
            dto.PurchaseQuantity != existing.PurchaseQuantity ||
            dto.ContentUnitId != existing.ContentUnitId ||
            dto.ContentQuantity != existing.ContentQuantity ||
            dto.BaseUnitId != existing.BaseUnitId;

        if (isStructuralChange)
            throw new ApiException(
                ErrorCodes.ArticleStructuralChangeNotAllowed,
                "This change would modify the article's structure (units/quantities), which is not allowed — use Supersede instead.",
                409);

        var p = new DynamicParameters();
        p.Add("@ArticleToken", dto.ArticleToken);
        p.Add("@Name", dto.Name);
        p.Add("@Description", dto.Description);
        p.Add("@SupplierSku", dto.SupplierSku);
        p.Add("@Barcode", dto.Barcode);
        p.Add("@Brand", dto.Brand);
        p.Add("@FamilyId", dto.FamilyId);
        p.Add("@SubFamilyId", dto.SubFamilyId);
        p.Add("@PurchaseUnitId", dto.PurchaseUnitId);
        p.Add("@PurchaseQuantity", dto.PurchaseQuantity);
        p.Add("@ContentUnitId", dto.ContentUnitId);
        p.Add("@ContentQuantity", dto.ContentQuantity);
        p.Add("@BaseUnitId", dto.BaseUnitId);
        p.Add("@MinimumOrderQty", dto.MinimumOrderQty);
        p.Add("@LeadTimeDays", dto.LeadTimeDays);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDto>(row);
    }

    public async Task<ArticleDto?> SupersedeAsync(Guid oldArticleToken, ArticleDto newArticleData, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = oldArticleToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!CanManage(context, existing.SupplierId))
            throw new ApiException(ErrorCodes.ArticleSupplierForbidden, "Not allowed to supersede articles for this supplier.", 403);

        if (existing.ReplacedByArticleId.HasValue)
            throw new ApiException(ErrorCodes.ArticleAlreadyReplaced, "This article has already been replaced.", 409);

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var createP = new DynamicParameters();
            createP.Add("@ArticleToken", Guid.NewGuid());
            createP.Add("@SupplierId", existing.SupplierId);
            createP.Add("@Name", newArticleData.Name);
            createP.Add("@Description", newArticleData.Description);
            createP.Add("@SupplierSku", newArticleData.SupplierSku);
            createP.Add("@Barcode", newArticleData.Barcode);
            createP.Add("@Brand", newArticleData.Brand);
            createP.Add("@FamilyId", newArticleData.FamilyId);
            createP.Add("@SubFamilyId", newArticleData.SubFamilyId);
            createP.Add("@PurchaseUnitId", newArticleData.PurchaseUnitId);
            createP.Add("@PurchaseQuantity", newArticleData.PurchaseQuantity);
            createP.Add("@ContentUnitId", newArticleData.ContentUnitId);
            createP.Add("@ContentQuantity", newArticleData.ContentQuantity);
            createP.Add("@BaseUnitId", newArticleData.BaseUnitId);
            createP.Add("@MinimumOrderQty", newArticleData.MinimumOrderQty);
            createP.Add("@LeadTimeDays", newArticleData.LeadTimeDays);
            createP.Add("@CreatedBy", context.ActorUserToken.ToString());

            var newRow = await connection.QueryFirstOrDefaultAsync<Article>(
                "sp_Article_Create", createP, transaction, commandType: CommandType.StoredProcedure);

            if (newRow is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var setReplacedP = new DynamicParameters();
            setReplacedP.Add("@ArticleToken", oldArticleToken);
            setReplacedP.Add("@ReplacedByArticleId", newRow.ArticleId);
            setReplacedP.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

            await connection.ExecuteAsync(
                "sp_Article_SetReplacedBy", setReplacedP, transaction, commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);
            return mapper.Map<ArticleDto>(newRow);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ArticleDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = token }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!CanManage(context, existing.SupplierId))
            throw new ApiException(ErrorCodes.ArticleSupplierForbidden, "Not allowed to change the active state of articles for this supplier.", 403);

        var p = new DynamicParameters();
        p.Add("@ArticleToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDto>(row);
    }

    public async Task<bool> DeleteAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", new { ArticleToken = token }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        if (!CanManage(context, existing.SupplierId))
            throw new ApiException(ErrorCodes.ArticleSupplierForbidden, "Not allowed to delete articles for this supplier.", 403);

        var p = new DynamicParameters();
        p.Add("@ArticleToken", token);
        p.Add("@DeletedBy", context.ActorUserToken.ToString());
        try
        {
            await connection.ExecuteAsync("sp_Article_SoftDelete", p, commandType: CommandType.StoredProcedure);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<BulkImportArticleResultDto> BulkImportArticlesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default)
    {
        // Unlike Users/Suppliers/Organizations (flat AdminRoleLevel gate), Articles is
        // ownership-scoped at the single-row level already (CanManage) — bulk import follows the
        // same shape rather than raising the floor above what a supplier can already do one row at
        // a time: the owning supplier may bulk-manage their own catalog, Admin+ may bulk-manage any
        // supplier's (resolved per row via the SupplierName column).
        if (!context.SupplierId.HasValue && context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ArticleBulkImportForbidden, "Only the owning supplier or Admins/SuperAdmins can bulk-import articles.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.ArticleBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.ArticleBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportArticleResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            await using var connection = connectionFactory.CreateConnection();

            var supplierCache = new Dictionary<string, Supplier?>(StringComparer.OrdinalIgnoreCase);
            var familyCache = new Dictionary<string, Family?>(StringComparer.OrdinalIgnoreCase);
            var subFamilyCache = new Dictionary<string, SubFamily?>(StringComparer.OrdinalIgnoreCase);
            var unitCache = new Dictionary<string, UnitOfMeasure?>(StringComparer.OrdinalIgnoreCase);

            async Task<UnitOfMeasure?> ResolveUnitAsync(string code)
            {
                var key = code.Trim().ToUpperInvariant();
                if (!unitCache.TryGetValue(key, out var unit))
                {
                    unit = await connection.QueryFirstOrDefaultAsync<UnitOfMeasure>(
                        "sp_UnitOfMeasure_GetByCode", new { Code = key }, commandType: CommandType.StoredProcedure);
                    unitCache[key] = unit;
                }
                return unit;
            }

            // IMPORTANT: rows are processed strictly sequentially — same convention as every other
            // bulk import in this codebase. Articles' uniqueness (SupplierId, NormalizedName) is a
            // real DB constraint, but sequential processing is what lets an insert-vs-update match
            // (SupplierId, SupplierSku) reflect an earlier row's insert within the same file, and
            // keeps row-numbered error reporting deterministic.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();

                var supplierName = row.Cell(1).GetString().Trim();
                var supplierSku = row.Cell(2).GetString().Trim();
                var name = row.Cell(3).GetString().Trim();
                var description = row.Cell(4).GetString().Trim();
                var barcode = row.Cell(5).GetString().Trim();
                var brand = row.Cell(6).GetString().Trim();
                var familyCode = row.Cell(7).GetString().Trim();
                var subFamilyCode = row.Cell(8).GetString().Trim();
                var purchaseUnitCode = row.Cell(9).GetString().Trim();
                var purchaseQuantityText = row.Cell(10).GetString().Trim();
                var contentUnitCode = row.Cell(11).GetString().Trim();
                var contentQuantityText = row.Cell(12).GetString().Trim();
                var baseUnitCode = row.Cell(13).GetString().Trim();
                var minimumOrderQtyText = row.Cell(14).GetString().Trim();
                var leadTimeDaysText = row.Cell(15).GetString().Trim();
                var articleTokenText = row.Cell(16).GetString().Trim();
                var statusText = row.Cell(17).GetString().Trim();

                var rowIdentifier = !string.IsNullOrWhiteSpace(supplierSku)
                    ? supplierSku
                    : (string.IsNullOrWhiteSpace(name) ? null : name);

                // --- Status: Active/Inactive/Deleted/blank. Deleted short-circuits below (before
                // Name/Family/Unit validation — deleting a row shouldn't require the rest of the
                // sheet to be well-formed); Active/Inactive are applied after a successful
                // insert/update. Values are literal English tokens, not localized — same decision
                // CLAUDE.md already made for the Status column's Active/Inactive display values on
                // export. ---
                string? normalizedStatus = null;
                if (!string.IsNullOrWhiteSpace(statusText))
                {
                    if (string.Equals(statusText, "Active", StringComparison.OrdinalIgnoreCase)) normalizedStatus = "Active";
                    else if (string.Equals(statusText, "Inactive", StringComparison.OrdinalIgnoreCase)) normalizedStatus = "Inactive";
                    else if (string.Equals(statusText, "Deleted", StringComparison.OrdinalIgnoreCase)) normalizedStatus = "Deleted";
                    else
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "Status must be Active, Inactive, Deleted, or left blank." });
                        continue;
                    }
                }

                // --- Resolve supplier ---
                int supplierId;
                if (context.SupplierId.HasValue)
                {
                    // A supplier-scoped caller's SupplierName cell is ignored (always forced to
                    // their own account) — but if they filled it in with something else, that's
                    // almost certainly a copy-paste mistake worth flagging rather than silently
                    // overriding, per the requirement that insert/update both stay self-scoped.
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
                            result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleSupplierForbidden, Description = "SupplierName does not match your own supplier account." });
                            continue;
                        }
                    }
                    supplierId = context.SupplierId.Value;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(supplierName))
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "SupplierName is required." });
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
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.SupplierNotFound, Description = $"Supplier '{supplierName}' was not found." });
                        continue;
                    }
                    supplierId = supplier.SupplierId;
                }

                // --- Resolve the existing article, if any. Moved ahead of Name/Family/Unit
                // validation (rather than immediately before insert/update below) so a
                // Status=Deleted row can short-circuit without requiring the rest of the sheet to
                // be well-formed — a caller flipping a batch of rows to Deleted on a re-uploaded
                // export shouldn't need every other column to still validate. ArticleToken
                // (populated when this row came from ExportArticlesAsync, i.e. the "export, edit,
                // re-upload" flow) is the primary, typo-proof match key — a mistyped
                // SupplierSku/Name can never silently update the wrong article or create an
                // unwanted duplicate when a token is present. A row with no token (freshly
                // hand-typed from the template) falls back to matching by (SupplierId,
                // SupplierSku); a blank SKU can never match an existing article there, so it's
                // always an insert. ---
                Article? existingArticle = null;
                if (!string.IsNullOrWhiteSpace(articleTokenText))
                {
                    if (!Guid.TryParse(articleTokenText, out var articleToken))
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "ArticleToken is not a valid identifier — leave it blank for a new article." });
                        continue;
                    }

                    existingArticle = await connection.QueryFirstOrDefaultAsync<Article>(
                        "sp_Article_GetByToken", new { ArticleToken = articleToken }, commandType: CommandType.StoredProcedure);

                    if (existingArticle is null)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = $"No article found for ArticleToken '{articleTokenText}'." });
                        continue;
                    }

                    if (existingArticle.SupplierId != supplierId)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleSupplierForbidden, Description = "ArticleToken does not belong to the resolved SupplierName — this row may have been copied from a different supplier's export." });
                        continue;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(supplierSku))
                {
                    existingArticle = await connection.QueryFirstOrDefaultAsync<Article>(
                        "sp_Article_GetBySupplierSku", new { SupplierId = supplierId, SupplierSku = supplierSku }, commandType: CommandType.StoredProcedure);
                }

                if (normalizedStatus == "Deleted")
                {
                    if (existingArticle is null)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "Status is Deleted but no existing article was matched (via ArticleToken or SupplierSku) — nothing to delete." });
                        continue;
                    }

                    try
                    {
                        var deleted = await DeleteAsync(existingArticle.ArticleToken, context, cancellationToken);
                        if (deleted)
                            result.DeletedCount++;
                        else
                            result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = "Article could not be deleted." });
                    }
                    catch (ApiException ex)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ex.Code, Description = ex.Message });
                    }
                    catch (Exception)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowFailed, Description = "An unexpected error occurred while processing this row." });
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "Name is required." });
                    continue;
                }

                // --- Resolve Family / SubFamily ---
                int? familyId = null;
                if (!string.IsNullOrWhiteSpace(familyCode))
                {
                    var familyKey = familyCode.ToUpperInvariant();
                    if (!familyCache.TryGetValue(familyKey, out var family))
                    {
                        family = await connection.QueryFirstOrDefaultAsync<Family>(
                            "sp_Family_GetByCode", new { Code = familyKey }, commandType: CommandType.StoredProcedure);
                        familyCache[familyKey] = family;
                    }
                    if (family is null)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.FamilyNotFound, Description = $"Family '{familyCode}' was not found." });
                        continue;
                    }
                    familyId = family.FamilyId;
                }

                int? subFamilyId = null;
                if (!string.IsNullOrWhiteSpace(subFamilyCode))
                {
                    // SubFamily.Code is only unique per-Family (UX_SubFamilies), not globally, so
                    // FamilyCode is required to resolve it unambiguously.
                    if (familyId is null)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "FamilyCode is required when SubFamilyCode is set." });
                        continue;
                    }

                    var subFamilyKey = $"{familyId}:{subFamilyCode.ToUpperInvariant()}";
                    if (!subFamilyCache.TryGetValue(subFamilyKey, out var subFamily))
                    {
                        subFamily = await connection.QueryFirstOrDefaultAsync<SubFamily>(
                            "sp_SubFamily_GetByCode", new { FamilyId = familyId, Code = subFamilyCode.ToUpperInvariant() }, commandType: CommandType.StoredProcedure);
                        subFamilyCache[subFamilyKey] = subFamily;
                    }
                    if (subFamily is null)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.SubFamilyNotFound, Description = $"Sub-family '{subFamilyCode}' was not found under family '{familyCode}'." });
                        continue;
                    }
                    subFamilyId = subFamily.SubFamilyId;
                }

                // --- Resolve units (same COUNT / WEIGHT-VOLUME / matching-type rules as the
                // single-row Create/Edit handlers) ---
                if (string.IsNullOrWhiteSpace(purchaseUnitCode))
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "PurchaseUnitCode is required." });
                    continue;
                }
                var purchaseUnit = await ResolveUnitAsync(purchaseUnitCode);
                if (purchaseUnit is null)
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.PurchaseUnitNotFound, Description = $"Purchase unit '{purchaseUnitCode}' was not found." });
                    continue;
                }
                if (!string.Equals(purchaseUnit.UnitTypeCode, UnitTypeCodes.Count, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.PurchaseUnitInvalidType, Description = "Purchase unit must be a COUNT unit (e.g. BOX, PACK, BAG)." });
                    continue;
                }

                if (!decimal.TryParse(purchaseQuantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var purchaseQuantity) || purchaseQuantity <= 0)
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "PurchaseQuantity is required and must be a positive number." });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(contentUnitCode))
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "ContentUnitCode is required." });
                    continue;
                }
                var contentUnit = await ResolveUnitAsync(contentUnitCode);
                if (contentUnit is null)
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ContentUnitNotFound, Description = $"Content unit '{contentUnitCode}' was not found." });
                    continue;
                }
                if (!string.Equals(contentUnit.UnitTypeCode, UnitTypeCodes.Weight, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(contentUnit.UnitTypeCode, UnitTypeCodes.Volume, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ContentUnitInvalidType, Description = "Content unit must be a WEIGHT or VOLUME unit." });
                    continue;
                }

                decimal? contentQuantity = null;
                if (!string.IsNullOrWhiteSpace(contentQuantityText))
                {
                    if (!decimal.TryParse(contentQuantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedContentQuantity) || parsedContentQuantity <= 0)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "ContentQuantity must be a positive number." });
                        continue;
                    }
                    contentQuantity = parsedContentQuantity;
                }

                int? baseUnitId = null;
                if (!string.IsNullOrWhiteSpace(baseUnitCode))
                {
                    var baseUnit = await ResolveUnitAsync(baseUnitCode);
                    if (baseUnit is null)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.BaseUnitNotFound, Description = $"Base unit '{baseUnitCode}' was not found." });
                        continue;
                    }
                    if (baseUnit.UnitTypeId != contentUnit.UnitTypeId)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.BaseUnitTypeMismatch, Description = "Base unit must be the same UnitType as the content unit (e.g. both WEIGHT or both VOLUME)." });
                        continue;
                    }
                    baseUnitId = baseUnit.UnitOfMeasureId;
                }

                decimal? minimumOrderQty = null;
                if (!string.IsNullOrWhiteSpace(minimumOrderQtyText))
                {
                    if (!decimal.TryParse(minimumOrderQtyText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMinQty) || parsedMinQty < 0)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "MinimumOrderQty must be a non-negative number." });
                        continue;
                    }
                    minimumOrderQty = parsedMinQty;
                }

                int? leadTimeDays = null;
                if (!string.IsNullOrWhiteSpace(leadTimeDaysText))
                {
                    if (!int.TryParse(leadTimeDaysText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLeadTime) || parsedLeadTime < 0)
                    {
                        result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowInvalid, Description = "LeadTimeDays must be a non-negative whole number." });
                        continue;
                    }
                    leadTimeDays = parsedLeadTime;
                }

                try
                {
                    if (existingArticle is not null)
                    {
                        // UPDATE path — structural fields are immutable via edit, same rule
                        // EditArticleCommandHandler enforces for the single-row edit endpoint. A row
                        // that tries to change them is rejected rather than silently applied or
                        // auto-superseded — bulk import is not the place for a structural change.
                        var isStructuralChange =
                            purchaseUnit.UnitOfMeasureId != existingArticle.PurchaseUnitId ||
                            purchaseQuantity != existingArticle.PurchaseQuantity ||
                            contentUnit.UnitOfMeasureId != existingArticle.ContentUnitId ||
                            contentQuantity != existingArticle.ContentQuantity ||
                            baseUnitId != existingArticle.BaseUnitId;

                        if (isStructuralChange)
                        {
                            result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleStructuralChangeNotAllowed, Description = "This row would change the article's structure (units/quantities), which bulk import does not allow — use Supersede instead." });
                            continue;
                        }

                        var updateDto = new ArticleDto
                        {
                            ArticleToken = existingArticle.ArticleToken,
                            SupplierId = existingArticle.SupplierId,
                            Name = name,
                            Description = NullIfEmpty(description),
                            SupplierSku = NullIfEmpty(supplierSku),
                            Barcode = NullIfEmpty(barcode),
                            Brand = NullIfEmpty(brand),
                            FamilyId = familyId,
                            SubFamilyId = subFamilyId,
                            PurchaseUnitId = purchaseUnit.UnitOfMeasureId,
                            PurchaseQuantity = purchaseQuantity,
                            ContentUnitId = contentUnit.UnitOfMeasureId,
                            ContentQuantity = contentQuantity,
                            BaseUnitId = baseUnitId,
                            MinimumOrderQty = minimumOrderQty,
                            LeadTimeDays = leadTimeDays
                        };

                        var updated = await EditAsync(updateDto, context, cancellationToken);
                        if (updated is null)
                        {
                            result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleNotFound, Description = "Article could not be updated." });
                            continue;
                        }

                        // normalizedStatus is never "Deleted" here — that case already
                        // short-circuited earlier in the loop before reaching this update path.
                        if (normalizedStatus is not null)
                        {
                            var desiredActive = normalizedStatus == "Active";
                            if (updated.IsActive != desiredActive)
                                await SetActiveAsync(existingArticle.ArticleToken, desiredActive, context, cancellationToken);
                        }

                        result.UpdatedCount++;
                    }
                    else
                    {
                        var createDto = new ArticleDto
                        {
                            SupplierId = supplierId,
                            Name = name,
                            Description = NullIfEmpty(description),
                            SupplierSku = NullIfEmpty(supplierSku),
                            Barcode = NullIfEmpty(barcode),
                            Brand = NullIfEmpty(brand),
                            FamilyId = familyId,
                            SubFamilyId = subFamilyId,
                            PurchaseUnitId = purchaseUnit.UnitOfMeasureId,
                            PurchaseQuantity = purchaseQuantity,
                            ContentUnitId = contentUnit.UnitOfMeasureId,
                            ContentQuantity = contentQuantity,
                            BaseUnitId = baseUnitId,
                            MinimumOrderQty = minimumOrderQty,
                            LeadTimeDays = leadTimeDays
                        };

                        var created = await CreateAsync(createDto, context, cancellationToken);
                        if (created is null)
                        {
                            result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleCreateFailed, Description = "Article could not be created." });
                            continue;
                        }

                        // A brand-new article is always created Active (CreateAsync's own,
                        // unchanged default) — Status=Inactive on an insert row deactivates it
                        // immediately afterward. Status=Deleted on an insert row was already
                        // rejected earlier in the loop (nothing to delete yet).
                        if (normalizedStatus == "Inactive")
                            await SetActiveAsync(created.ArticleToken, false, context, cancellationToken);

                        result.InsertedCount++;
                    }
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportArticleRowErrorDto { RowNumber = rowNumber, Identifier = rowIdentifier, Code = ErrorCodes.ArticleBulkImportRowFailed, Description = "An unexpected error occurred while processing this row." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportArticlesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!context.SupplierId.HasValue && context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ArticleBulkImportForbidden, "Only the owning supplier or Admins/SuperAdmins can export articles.", 403);

        // No supplierId/familyId/subFamilyId filter here — GetPagedAsync's own visibility rule
        // already forces a supplier-scoped caller to their own catalog; an Admin+ caller exports
        // the full catalog, matching the single-row read scope.
        var articles = await GetPagedAsync(1, MaxExportRows, null, null, null, searchText, includeInactive, context, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Articles");

        // ArticleToken sits right after the fields that also appear on the import template (same
        // column position, 16) so a re-uploaded export lines up with a hand-filled template file;
        // Status is export-only and trails after it, where the importer already ignores it.
        string[] headers = ["SupplierName", "SupplierSku", "Name", "Description", "Barcode", "Brand", "FamilyCode", "SubFamilyCode", "PurchaseUnitCode", "PurchaseQuantity", "ContentUnitCode", "ContentQuantity", "BaseUnitCode", "MinimumOrderQty", "LeadTimeDays", "ArticleToken", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var article in articles.Items)
        {
            worksheet.Cell(r, 1).Value = article.SupplierName;
            worksheet.Cell(r, 2).Value = article.SupplierSku;
            worksheet.Cell(r, 3).Value = article.Name;
            worksheet.Cell(r, 4).Value = article.Description;
            worksheet.Cell(r, 5).Value = article.Barcode;
            worksheet.Cell(r, 6).Value = article.Brand;
            worksheet.Cell(r, 7).Value = article.FamilyCode;
            worksheet.Cell(r, 8).Value = article.SubFamilyCode;
            worksheet.Cell(r, 9).Value = article.PurchaseUnitCode;
            worksheet.Cell(r, 10).Value = article.PurchaseQuantity;
            worksheet.Cell(r, 11).Value = article.ContentUnitCode;
            worksheet.Cell(r, 12).Value = article.ContentQuantity;
            worksheet.Cell(r, 13).Value = article.BaseUnitCode;
            worksheet.Cell(r, 14).Value = article.MinimumOrderQty;
            worksheet.Cell(r, 15).Value = article.LeadTimeDays;
            worksheet.Cell(r, 16).Value = article.ArticleToken.ToString();
            worksheet.Cell(r, 17).Value = article.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"articles_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateArticleImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (!context.SupplierId.HasValue && context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.ArticleBulkImportForbidden, "Only the owning supplier or Admins/SuperAdmins can download the import template.", 403);

        using var workbook = new XLWorkbook();

        var articlesSheet = workbook.Worksheets.Add("Articles");
        // ArticleToken (column 16) and Status (column 17) are left blank on a fresh template —
        // every row is a new article, so there's nothing to match and no status to change yet.
        // Both exist here purely so the columns line up with ExportArticlesAsync's layout: a
        // caller who re-uploads their own export (the recommended update flow) gets
        // ArticleToken-based matching and Status-driven activate/deactivate/delete for free,
        // instead of relying on SupplierSku and having no bulk lifecycle-state control at all.
        string[] headers = ["SupplierName", "SupplierSku", "Name", "Description", "Barcode", "Brand", "FamilyCode", "SubFamilyCode", "PurchaseUnitCode", "PurchaseQuantity", "ContentUnitCode", "ContentQuantity", "BaseUnitCode", "MinimumOrderQty", "LeadTimeDays", "ArticleToken", "Status"];
        for (var i = 0; i < headers.Length; i++)
            articlesSheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        articlesSheet.Row(1).Style.Font.Bold = true;

        // Suppliers reference sheet + dropdown — only wired up for an Admin+ actor, who must
        // specify which supplier each row belongs to. A supplier-scoped caller's SupplierName cell
        // is ignored on import (always forced to their own account), so there's nothing useful to
        // pick from a dropdown for them.
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
                var namedRange = workbook.DefinedNames.Add("ArticleImportSupplierNames", suppliersSheet.Range(2, 1, supplierRow - 1, 1));
                articlesSheet.Range(2, 1, MaxBulkImportRows + 1, 1).CreateDataValidation().List(namedRange.Name, true);
            }
        }

        var families = await familyService.GetPagedAsync(1, MaxExportRows, null, false, cancellationToken);
        var familiesSheet = workbook.Worksheets.Add("Families");
        familiesSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Code", language);
        familiesSheet.Row(1).Style.Font.Bold = true;
        var familyRow = 2;
        foreach (var family in families.Items.OrderBy(f => f.Code))
            familiesSheet.Cell(familyRow++, 1).Value = family.Code;
        familiesSheet.Columns().AdjustToContents();

        if (families.Items.Count > 0)
        {
            var familyNamedRange = workbook.DefinedNames.Add("ArticleImportFamilyCodes", familiesSheet.Range(2, 1, familyRow - 1, 1));
            articlesSheet.Range(2, 7, MaxBulkImportRows + 1, 7).CreateDataValidation().List(familyNamedRange.Name, true);
        }

        // SubFamilies reference sheet lists FamilyCode alongside SubFamilyCode since Code is only
        // unique per-family (not globally) — the dropdown itself can't be dynamically filtered by
        // the row's own FamilyCode cell (Excel has no cross-cell List validation without VBA), so
        // this is a visual grouping aid; BulkImportArticlesAsync still resolves (FamilyCode,
        // SubFamilyCode) together and requires FamilyCode whenever SubFamilyCode is filled in.
        var subFamilies = await subFamilyService.GetPagedAsync(1, MaxExportRows, null, null, false, cancellationToken);
        var subFamiliesSheet = workbook.Worksheets.Add("SubFamilies");
        subFamiliesSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("FamilyCode", language);
        subFamiliesSheet.Cell(1, 2).Value = BulkExcelLocalization.Header("SubFamilyCode", language);
        subFamiliesSheet.Row(1).Style.Font.Bold = true;
        var subFamilyRow = 2;
        var familyCodesById = families.Items.ToDictionary(f => f.FamilyId, f => f.Code);
        foreach (var subFamily in subFamilies.Items.OrderBy(sf => familyCodesById.GetValueOrDefault(sf.FamilyId, "")).ThenBy(sf => sf.Code))
        {
            subFamiliesSheet.Cell(subFamilyRow, 1).Value = familyCodesById.GetValueOrDefault(subFamily.FamilyId, "");
            subFamiliesSheet.Cell(subFamilyRow, 2).Value = subFamily.Code;
            subFamilyRow++;
        }
        subFamiliesSheet.Columns().AdjustToContents();

        if (subFamilies.Items.Count > 0)
        {
            var subFamilyNamedRange = workbook.DefinedNames.Add("ArticleImportSubFamilyCodes", subFamiliesSheet.Range(2, 2, subFamilyRow - 1, 2));
            articlesSheet.Range(2, 8, MaxBulkImportRows + 1, 8).CreateDataValidation().List(subFamilyNamedRange.Name, true);
        }

        // Units reference sheet backs all three unit columns (Purchase/Content/Base) — Excel can't
        // restrict a dropdown to only COUNT (for Purchase) or WEIGHT/VOLUME (for Content/Base)
        // units without VBA, so all three share the same full list; the UnitType column is a
        // visual hint for whoever fills the sheet, and BulkImportArticlesAsync still validates the
        // resolved type server-side.
        var units = await unitOfMeasureService.GetPagedAsync(1, MaxExportRows, null, false, cancellationToken);
        var unitsSheet = workbook.Worksheets.Add("Units");
        unitsSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Code", language);
        unitsSheet.Cell(1, 2).Value = BulkExcelLocalization.Header("UnitType", language);
        unitsSheet.Row(1).Style.Font.Bold = true;
        var unitRow = 2;
        foreach (var unit in units.Items.OrderBy(u => u.UnitTypeCode).ThenBy(u => u.Code))
        {
            unitsSheet.Cell(unitRow, 1).Value = unit.Code;
            unitsSheet.Cell(unitRow, 2).Value = unit.UnitTypeCode;
            unitRow++;
        }
        unitsSheet.Columns().AdjustToContents();

        if (units.Items.Count > 0)
        {
            var unitNamedRange = workbook.DefinedNames.Add("ArticleImportUnitCodes", unitsSheet.Range(2, 1, unitRow - 1, 1));
            articlesSheet.Range(2, 9, MaxBulkImportRows + 1, 9).CreateDataValidation().List(unitNamedRange.Name, true);
            articlesSheet.Range(2, 11, MaxBulkImportRows + 1, 11).CreateDataValidation().List(unitNamedRange.Name, true);
            articlesSheet.Range(2, 13, MaxBulkImportRows + 1, 13).CreateDataValidation().List(unitNamedRange.Name, true);
        }

        articlesSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), "articles_import_template.xlsx");
    }
}
