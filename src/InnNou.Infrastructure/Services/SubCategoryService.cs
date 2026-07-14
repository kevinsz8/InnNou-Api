using ClosedXML.Excel;
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

public class SubCategoryService(IDbConnectionFactory connectionFactory, IMapper mapper, ICategoryService categoryService) : ISubCategoryService
{
    private sealed class SubCategoryPageRow : SubCategory { public int TotalCount { get; set; } }

    private const int AdminRoleLevel = 80;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    public async Task<PagedResult<SubCategoryDto>> GetPagedAsync(int pageNumber, int pageSize, int? categoryId = null, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@CategoryId", categoryId);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<SubCategoryPageRow>(
            "sp_SubCategory_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<SubCategoryDto>
        {
            Items = mapper.MapList<SubCategoryDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<SubCategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int categoryId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@CategoryId", categoryId);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_SubCategory_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<SubCategoryDto?> CreateAsync(SubCategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", Guid.NewGuid());
        p.Add("@CategoryId", dto.CategoryId);
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }

    public async Task<SubCategoryDto?> EditAsync(SubCategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", dto.SubCategoryToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }

    public async Task<SubCategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }

    public async Task<BulkImportSubCategoryResultDto> BulkImportSubCategoriesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SubCategoryBulkImportForbidden, "Only Admins and SuperAdmins can bulk-import sub-categories.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.SubCategoryBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.SubCategoryBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportSubCategoryResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            await using var connection = connectionFactory.CreateConnection();
            var categoryCache = new Dictionary<string, Category?>(StringComparer.OrdinalIgnoreCase);

            // IMPORTANT: rows processed strictly sequentially, same convention as every bulk import
            // in this codebase — SubCategories uniqueness (UX_SubCategories on CategoryId+Code) is a
            // real DB constraint, so this is about deterministic row-numbered errors, not preventing
            // duplicates under parallelism.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();
                var categoryCode = row.Cell(1).GetString().Trim();
                var code = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(categoryCode))
                {
                    result.Errors.Add(new BulkImportSubCategoryRowErrorDto { RowNumber = rowNumber, SubCategoryCode = string.IsNullOrWhiteSpace(code) ? null : code, Code = ErrorCodes.SubCategoryBulkImportRowInvalid, Description = "CategoryCode is required." });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new BulkImportSubCategoryRowErrorDto { RowNumber = rowNumber, SubCategoryCode = null, Code = ErrorCodes.SubCategoryBulkImportRowInvalid, Description = "Code is required." });
                    continue;
                }

                var categoryKey = categoryCode.ToUpperInvariant();
                if (!categoryCache.TryGetValue(categoryKey, out var category))
                {
                    category = await connection.QueryFirstOrDefaultAsync<Category>(
                        "sp_Category_GetByCode", new { Code = categoryKey }, commandType: CommandType.StoredProcedure);
                    categoryCache[categoryKey] = category;
                }
                if (category is null)
                {
                    result.Errors.Add(new BulkImportSubCategoryRowErrorDto { RowNumber = rowNumber, SubCategoryCode = code, Code = ErrorCodes.CategoryNotFound, Description = $"Category '{categoryCode}' was not found." });
                    continue;
                }

                if (await ExistsByCodeAsync(code, category.CategoryId, cancellationToken))
                {
                    result.Errors.Add(new BulkImportSubCategoryRowErrorDto { RowNumber = rowNumber, SubCategoryCode = code, Code = ErrorCodes.SubCategoryCodeExists, Description = "A sub-category with this code already exists under this category." });
                    continue;
                }

                try
                {
                    var created = await CreateAsync(new SubCategoryDto { CategoryId = category.CategoryId, Code = code }, cancellationToken);
                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportSubCategoryRowErrorDto { RowNumber = rowNumber, SubCategoryCode = code, Code = ErrorCodes.SubCategoryCreateFailed, Description = "Sub-category creation failed." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportSubCategoryRowErrorDto { RowNumber = rowNumber, SubCategoryCode = code, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportSubCategoryRowErrorDto { RowNumber = rowNumber, SubCategoryCode = code, Code = ErrorCodes.SubCategoryBulkImportRowFailed, Description = "An unexpected error occurred while creating this sub-category." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportSubCategoriesAsync(string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SubCategoryBulkImportForbidden, "Only Admins and SuperAdmins can export sub-categories.", 403);

        var subCategories = await GetPagedAsync(1, MaxExportRows, null, searchText, includeInactive, cancellationToken);
        var categories = await categoryService.GetPagedAsync(1, MaxExportRows, null, true, cancellationToken);
        var categoryCodesById = categories.Items.ToDictionary(c => c.CategoryId, c => c.Code);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("SubCategories");

        string[] headers = ["CategoryCode", "Code", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var subCategory in subCategories.Items)
        {
            worksheet.Cell(r, 1).Value = categoryCodesById.GetValueOrDefault(subCategory.CategoryId, "");
            worksheet.Cell(r, 2).Value = subCategory.Code;
            worksheet.Cell(r, 3).Value = subCategory.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"subcategories_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateSubCategoryImportTemplateAsync(IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SubCategoryBulkImportForbidden, "Only Admins and SuperAdmins can download the import template.", 403);

        var categories = await categoryService.GetPagedAsync(1, MaxExportRows, null, false, cancellationToken);

        using var workbook = new XLWorkbook();

        var subCategoriesSheet = workbook.Worksheets.Add("SubCategories");
        string[] headers = ["CategoryCode", "Code"];
        for (var i = 0; i < headers.Length; i++)
            subCategoriesSheet.Cell(1, i + 1).Value = headers[i];
        subCategoriesSheet.Row(1).Style.Font.Bold = true;

        var categoriesSheet = workbook.Worksheets.Add("Categories");
        categoriesSheet.Cell(1, 1).Value = "Code";
        categoriesSheet.Row(1).Style.Font.Bold = true;
        var categoryRow = 2;
        foreach (var category in categories.Items.OrderBy(c => c.Code))
            categoriesSheet.Cell(categoryRow++, 1).Value = category.Code;
        categoriesSheet.Columns().AdjustToContents();

        // Restricts the CategoryCode column to a dropdown sourced from "Categories" — same
        // named-range technique as Organizations' ParentOrganizationName dropdown. Only wired up
        // when there's at least one category to list.
        if (categories.Items.Count > 0)
        {
            var namedRange = workbook.DefinedNames.Add("SubCategoryImportCategoryCodes", categoriesSheet.Range(2, 1, categoryRow - 1, 1));
            subCategoriesSheet.Range(2, 1, MaxBulkImportRows + 1, 1).CreateDataValidation().List(namedRange.Name, true);
        }

        subCategoriesSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), "subcategories_import_template.xlsx");
    }
}
