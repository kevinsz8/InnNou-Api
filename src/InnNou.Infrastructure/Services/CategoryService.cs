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

    // Category/SubCategory Create/Edit have no RoleLevel gate at all today (open to any
    // authenticated user, enforced only by .RequireAuthorization() at the endpoint) — bulk import
    // introduces the first one, at AdminRoleLevel, matching the "bulk is a higher-trust capability"
    // policy already established for every other entity (e.g. Organizations gates bulk stricter
    // than its own single-row create allows).
    private const int AdminRoleLevel = 80;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    public async Task<PagedResult<CategoryDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);
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

    public async Task<CategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Category_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<CategoryDto?> CreateAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", Guid.NewGuid());
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<CategoryDto?> EditAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", dto.CategoryToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<CategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
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
            // uniqueness (UQ_Categories_Code) is a real DB constraint, so this is about consistency
            // and predictable errors, not preventing duplicate inserts under parallelism.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();
                var code = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new BulkImportCategoryRowErrorDto { RowNumber = rowNumber, CategoryCode = null, Code = ErrorCodes.CategoryBulkImportRowInvalid, Description = "Code is required." });
                    continue;
                }

                if (await ExistsByCodeAsync(code, cancellationToken))
                {
                    result.Errors.Add(new BulkImportCategoryRowErrorDto { RowNumber = rowNumber, CategoryCode = code, Code = ErrorCodes.CategoryCodeExists, Description = "A category with this code already exists." });
                    continue;
                }

                try
                {
                    var created = await CreateAsync(new CategoryDto { Code = code }, cancellationToken);
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

        var categories = await GetPagedAsync(1, MaxExportRows, searchText, includeInactive, cancellationToken);

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
