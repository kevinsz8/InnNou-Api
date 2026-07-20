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

namespace InnNou.Infrastructure.Services;

public class CategoryService(IDbConnectionFactory connectionFactory, IMapper mapper) : ICategoryService
{
    private sealed class CategoryPageRow : Category { public int TotalCount { get; set; } }

    private const int MaxPageSize = 100;
    private const int StaffRoleLevel = 20;
    private const int AdminRoleLevel = 80;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    // Resolves the OrganizationId a new category should be anchored to, or null for
    // global. SuperAdmin may target any org via organizationToken (or omit it for
    // global); a Super Asociado's own Staff+ can only ever anchor to their own
    // context.OrganizationId — any client-supplied organizationToken is ignored for
    // them, mirroring SupplierService's "Staff+ creates private supplier scoped only
    // to context.OrganizationId" rule.
    private async Task<int?> ResolveWriteOwnerOrganizationIdAsync(
        IDbConnection connection, IRequestContext context, Guid? organizationToken)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            if (!organizationToken.HasValue)
                return null;

            var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                "sp_Organization_GetByToken",
                new { OrganizationToken = organizationToken.Value },
                commandType: CommandType.StoredProcedure);

            if (organization is null)
                throw new ApiException(ErrorCodes.CategoryOrganizationNotFound, "The specified owning organization was not found.", 404);

            return organization.OrganizationId;
        }

        if (context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate
            && context.RoleLevel >= StaffRoleLevel
            && context.OrganizationId.HasValue)
        {
            return context.OrganizationId.Value;
        }

        throw new ApiException(ErrorCodes.CategoryCreateForbidden, "Insufficient permissions to create a category.", 403);
    }

    private static void EnsureCanWriteCategory(IRequestContext context, int? categoryOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel) return;

        var isOwnSuperAssociate = context.OrganizationTypeCode == OrganizationTypeCodes.SuperAssociate
            && context.RoleLevel >= StaffRoleLevel
            && context.OrganizationId.HasValue
            && categoryOrganizationId == context.OrganizationId.Value;

        if (!isOwnSuperAssociate)
            throw new ApiException(ErrorCodes.CategoryOutsideScope, "Cannot edit a category outside your organization's scope.", 403);
    }

    public async Task<PagedResult<CategoryDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText, bool includeInactive, IRequestContext context, bool unrestricted = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);
        p.Add("@ContextRoleLevel", unrestricted ? SuperAdminRoleLevel : context.RoleLevel);
        p.Add("@ContextOrganizationId", unrestricted ? null : context.OrganizationId);
        var rows = (await connection.QueryAsync<CategoryPageRow>(
            "sp_Category_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<CategoryDto>
        {
            Items = mapper.MapList<CategoryDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<CategoryDto?> GetByTokenAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", token);
        p.Add("@ContextRoleLevel", context.RoleLevel);
        p.Add("@ContextOrganizationId", context.OrganizationId);
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? organizationId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@OrganizationId", organizationId);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Category_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<CategoryDto?> CreateAsync(CategoryDto dto, IRequestContext context, bool bypassAuthorization = false, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        // bypassAuthorization is set only by BulkImportCategoriesAsync, which is a
        // deliberately global-only capability regardless of the importing Admin's own
        // organization — do not repurpose this as a generic auth-skip elsewhere.
        var organizationId = bypassAuthorization
            ? (int?)null
            : await ResolveWriteOwnerOrganizationIdAsync(connection, context, dto.OrganizationToken);

        if (await ExistsByCodeAsync(dto.Code, organizationId, cancellationToken))
            throw new ApiException(ErrorCodes.CategoryCodeExists, "A category with this code already exists.", 409);

        var p = new DynamicParameters();
        p.Add("@CategoryToken", Guid.NewGuid());
        p.Add("@Code", dto.Code);
        p.Add("@OrganizationId", organizationId);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<CategoryDto?> EditAsync(CategoryDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        // Fetch unrestricted (only @CategoryToken passed, relying on the SP's
        // @ContextRoleLevel = 100 default) — not the visibility-filtered read path.
        // Reusing the filtered read would collapse a real-but-out-of-scope row and a
        // genuinely nonexistent one into the same null, turning a 403 into a
        // misleading 404.
        var existing = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_GetByToken", new { CategoryToken = dto.CategoryToken }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        EnsureCanWriteCategory(context, existing.OrganizationId);

        var p = new DynamicParameters();
        p.Add("@CategoryToken", dto.CategoryToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<CategoryDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_GetByToken", new { CategoryToken = token }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        EnsureCanWriteCategory(context, existing.OrganizationId);

        var p = new DynamicParameters();
        p.Add("@CategoryToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<BulkImportCategoryResultDto> BulkImportCategoriesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.CategoryBulkImportForbidden, "Only Admins and SuperAdmins can bulk-import categories.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.CategoryBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.CategoryBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportCategoryResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            // IMPORTANT: rows are processed strictly sequentially — same convention as every other
            // bulk import in this codebase — so each row's ExistsByCodeAsync check reflects the
            // previous row's insert and row-numbered error reporting stays deterministic. Categories
            // uniqueness (UX_Categories_Global) is a real DB constraint, so this is about
            // consistency and predictable errors, not preventing duplicate inserts under parallelism.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();
                var code = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new BulkImportCategoryRowErrorDto { RowNumber = rowNumber, CategoryCode = null, Code = ErrorCodes.CategoryBulkImportRowInvalid, Description = "Code is required." });
                    continue;
                }

                if (await ExistsByCodeAsync(code, null, cancellationToken))
                {
                    result.Errors.Add(new BulkImportCategoryRowErrorDto { RowNumber = rowNumber, CategoryCode = code, Code = ErrorCodes.CategoryCodeExists, Description = "A category with this code already exists." });
                    continue;
                }

                try
                {
                    var created = await CreateAsync(new CategoryDto { Code = code }, context, bypassAuthorization: true, cancellationToken);
                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportCategoryRowErrorDto { RowNumber = rowNumber, CategoryCode = code, Code = ErrorCodes.CategoryCreateFailed, Description = "Category creation failed." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportCategoryRowErrorDto { RowNumber = rowNumber, CategoryCode = code, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportCategoryRowErrorDto { RowNumber = rowNumber, CategoryCode = code, Code = ErrorCodes.CategoryBulkImportRowFailed, Description = "An unexpected error occurred while creating this category." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportCategoriesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.CategoryBulkImportForbidden, "Only Admins and SuperAdmins can export categories.", 403);

        var categories = await GetPagedAsync(1, MaxExportRows, searchText, includeInactive, context, unrestricted: true, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Categories");

        string[] headers = ["Code", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var category in categories.Items)
        {
            worksheet.Cell(r, 1).Value = category.Code;
            worksheet.Cell(r, 2).Value = category.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"categories_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public Task<(byte[] FileBytes, string FileName)> GenerateCategoryImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.CategoryBulkImportForbidden, "Only Admins and SuperAdmins can download the import template.", 403);

        // No reference sheet needed — Categories has no name-resolved FK in its import columns
        // (just Code), unlike SubCategories which needs a CategoryCode dropdown.
        using var workbook = new XLWorkbook();

        var categoriesSheet = workbook.Worksheets.Add("Categories");
        categoriesSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Code", language);
        categoriesSheet.Row(1).Style.Font.Bold = true;
        categoriesSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return Task.FromResult((ms.ToArray(), "categories_import_template.xlsx"));
    }
}
