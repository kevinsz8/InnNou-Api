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

public class SubFamilyService(IDbConnectionFactory connectionFactory, IMapper mapper, IFamilyService familyService) : ISubFamilyService
{
    private sealed class SubFamilyPageRow : SubFamily { public int TotalCount { get; set; } }

    private const int AdminRoleLevel = 80;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    public async Task<PagedResult<SubFamilyDto>> GetPagedAsync(int pageNumber, int pageSize, int? familyId = null, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@FamilyId", familyId);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<SubFamilyPageRow>(
            "sp_SubFamily_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<SubFamilyDto>
        {
            Items = mapper.MapList<SubFamilyDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<SubFamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubFamilyToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int familyId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@FamilyId", familyId);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_SubFamily_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<SubFamilyDto?> CreateAsync(SubFamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubFamilyToken", Guid.NewGuid());
        p.Add("@FamilyId", dto.FamilyId);
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }

    public async Task<SubFamilyDto?> EditAsync(SubFamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubFamilyToken", dto.SubFamilyToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }

    public async Task<SubFamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubFamilyToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubFamily>(
            "sp_SubFamily_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubFamilyDto>(row);
    }

    public async Task<BulkImportSubFamilyResultDto> BulkImportSubFamiliesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SubFamilyBulkImportForbidden, "Only Admins and SuperAdmins can bulk-import sub-families.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.SubFamilyBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.SubFamilyBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportSubFamilyResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            await using var connection = connectionFactory.CreateConnection();
            var familyCache = new Dictionary<string, Family?>(StringComparer.OrdinalIgnoreCase);

            // IMPORTANT: rows processed strictly sequentially, same convention as every bulk import
            // in this codebase — SubFamilies uniqueness (UX_SubFamilies on FamilyId+Code) is a real
            // DB constraint, so this is about deterministic row-numbered errors, not preventing
            // duplicates under parallelism.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();
                var familyCode = row.Cell(1).GetString().Trim();
                var code = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(familyCode))
                {
                    result.Errors.Add(new BulkImportSubFamilyRowErrorDto { RowNumber = rowNumber, SubFamilyCode = string.IsNullOrWhiteSpace(code) ? null : code, Code = ErrorCodes.SubFamilyBulkImportRowInvalid, Description = "FamilyCode is required." });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new BulkImportSubFamilyRowErrorDto { RowNumber = rowNumber, SubFamilyCode = null, Code = ErrorCodes.SubFamilyBulkImportRowInvalid, Description = "Code is required." });
                    continue;
                }

                var familyKey = familyCode.ToUpperInvariant();
                if (!familyCache.TryGetValue(familyKey, out var family))
                {
                    family = await connection.QueryFirstOrDefaultAsync<Family>(
                        "sp_Family_GetByCode", new { Code = familyKey }, commandType: CommandType.StoredProcedure);
                    familyCache[familyKey] = family;
                }
                if (family is null)
                {
                    result.Errors.Add(new BulkImportSubFamilyRowErrorDto { RowNumber = rowNumber, SubFamilyCode = code, Code = ErrorCodes.FamilyNotFound, Description = $"Family '{familyCode}' was not found." });
                    continue;
                }

                if (await ExistsByCodeAsync(code, family.FamilyId, cancellationToken))
                {
                    result.Errors.Add(new BulkImportSubFamilyRowErrorDto { RowNumber = rowNumber, SubFamilyCode = code, Code = ErrorCodes.SubFamilyCodeExists, Description = "A sub-family with this code already exists under this family." });
                    continue;
                }

                try
                {
                    var created = await CreateAsync(new SubFamilyDto { FamilyId = family.FamilyId, Code = code }, cancellationToken);
                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportSubFamilyRowErrorDto { RowNumber = rowNumber, SubFamilyCode = code, Code = ErrorCodes.SubFamilyCreateFailed, Description = "Sub-family creation failed." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportSubFamilyRowErrorDto { RowNumber = rowNumber, SubFamilyCode = code, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportSubFamilyRowErrorDto { RowNumber = rowNumber, SubFamilyCode = code, Code = ErrorCodes.SubFamilyBulkImportRowFailed, Description = "An unexpected error occurred while creating this sub-family." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportSubFamiliesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SubFamilyBulkImportForbidden, "Only Admins and SuperAdmins can export sub-families.", 403);

        var subFamilies = await GetPagedAsync(1, MaxExportRows, null, searchText, includeInactive, cancellationToken);
        var families = await familyService.GetPagedAsync(1, MaxExportRows, null, true, cancellationToken);
        var familyCodesById = families.Items.ToDictionary(f => f.FamilyId, f => f.Code);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("SubFamilies");

        string[] headers = ["FamilyCode", "Code", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var subFamily in subFamilies.Items)
        {
            worksheet.Cell(r, 1).Value = familyCodesById.GetValueOrDefault(subFamily.FamilyId, "");
            worksheet.Cell(r, 2).Value = subFamily.Code;
            worksheet.Cell(r, 3).Value = subFamily.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"subfamilies_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateSubFamilyImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SubFamilyBulkImportForbidden, "Only Admins and SuperAdmins can download the import template.", 403);

        var families = await familyService.GetPagedAsync(1, MaxExportRows, null, false, cancellationToken);

        using var workbook = new XLWorkbook();

        var subFamiliesSheet = workbook.Worksheets.Add("SubFamilies");
        string[] headers = ["FamilyCode", "Code"];
        for (var i = 0; i < headers.Length; i++)
            subFamiliesSheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        subFamiliesSheet.Row(1).Style.Font.Bold = true;

        var familiesSheet = workbook.Worksheets.Add("Families");
        familiesSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Code", language);
        familiesSheet.Row(1).Style.Font.Bold = true;
        var familyRow = 2;
        foreach (var family in families.Items.OrderBy(f => f.Code))
            familiesSheet.Cell(familyRow++, 1).Value = family.Code;
        familiesSheet.Columns().AdjustToContents();

        // Restricts the FamilyCode column to a dropdown sourced from "Families" — same named-range
        // technique as SubCategory's CategoryCode dropdown / Organizations' ParentOrganizationName.
        if (families.Items.Count > 0)
        {
            var namedRange = workbook.DefinedNames.Add("SubFamilyImportFamilyCodes", familiesSheet.Range(2, 1, familyRow - 1, 1));
            subFamiliesSheet.Range(2, 1, MaxBulkImportRows + 1, 1).CreateDataValidation().List(namedRange.Name, true);
        }

        subFamiliesSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), "subfamilies_import_template.xlsx");
    }
}
