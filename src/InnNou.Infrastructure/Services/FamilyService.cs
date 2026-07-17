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

public class FamilyService(IDbConnectionFactory connectionFactory, IMapper mapper) : IFamilyService
{
    private sealed class FamilyPageRow : Family { public int TotalCount { get; set; } }

    // Family/SubFamily Create/Edit have no RoleLevel gate at all today (open to any
    // authenticated user) — bulk import introduces the first one, at AdminRoleLevel, matching
    // Category/SubCategory's identical bulk-import gate.
    private const int AdminRoleLevel = 80;
    private const int MaxPageSize = 100;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    public async Task<PagedResult<FamilyDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<FamilyPageRow>(
            "sp_Family_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<FamilyDto>
        {
            Items = mapper.MapList<FamilyDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<FamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Family_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<FamilyDto?> CreateAsync(FamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyToken", Guid.NewGuid());
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }

    public async Task<FamilyDto?> EditAsync(FamilyDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyToken", dto.FamilyToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }

    public async Task<FamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@FamilyToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyDto>(row);
    }

    public async Task<BulkImportFamilyResultDto> BulkImportFamiliesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.FamilyBulkImportForbidden, "Only Admins and SuperAdmins can bulk-import families.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.FamilyBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.FamilyBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportFamilyResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            // IMPORTANT: rows processed strictly sequentially, same convention as every bulk import
            // in this codebase — Families uniqueness (UQ_Families_Code) is a real DB constraint, so
            // this is about deterministic row-numbered errors, not preventing duplicates under
            // parallelism.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();
                var code = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add(new BulkImportFamilyRowErrorDto { RowNumber = rowNumber, FamilyCode = null, Code = ErrorCodes.FamilyBulkImportRowInvalid, Description = "Code is required." });
                    continue;
                }

                if (await ExistsByCodeAsync(code, cancellationToken))
                {
                    result.Errors.Add(new BulkImportFamilyRowErrorDto { RowNumber = rowNumber, FamilyCode = code, Code = ErrorCodes.FamilyCodeExists, Description = "A family with this code already exists." });
                    continue;
                }

                try
                {
                    var created = await CreateAsync(new FamilyDto { Code = code }, cancellationToken);
                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportFamilyRowErrorDto { RowNumber = rowNumber, FamilyCode = code, Code = ErrorCodes.FamilyCreateFailed, Description = "Family creation failed." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportFamilyRowErrorDto { RowNumber = rowNumber, FamilyCode = code, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportFamilyRowErrorDto { RowNumber = rowNumber, FamilyCode = code, Code = ErrorCodes.FamilyBulkImportRowFailed, Description = "An unexpected error occurred while creating this family." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportFamiliesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.FamilyBulkImportForbidden, "Only Admins and SuperAdmins can export families.", 403);

        var families = await GetPagedAsync(1, MaxExportRows, searchText, includeInactive, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Families");

        string[] headers = ["Code", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var family in families.Items)
        {
            worksheet.Cell(r, 1).Value = family.Code;
            worksheet.Cell(r, 2).Value = family.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"families_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public Task<(byte[] FileBytes, string FileName)> GenerateFamilyImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.FamilyBulkImportForbidden, "Only Admins and SuperAdmins can download the import template.", 403);

        using var workbook = new XLWorkbook();

        var familiesSheet = workbook.Worksheets.Add("Families");
        familiesSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Code", language);
        familiesSheet.Row(1).Style.Font.Bold = true;
        familiesSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return Task.FromResult((ms.ToArray(), "families_import_template.xlsx"));
    }
}
