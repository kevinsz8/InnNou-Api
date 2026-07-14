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

public class OrganizationService(IDbConnectionFactory connectionFactory, IMapper mapper, ICurrencyService currencyService) : IOrganizationService
{
    private sealed class OrganizationPageRow : Organization { public int TotalCount { get; set; } }

    private const int SuperAdminRoleLevel = 100;
    private const int AdminRoleLevel = 80;
    private const int ManagerRoleLevel = 60;

    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    private enum OrganizationScope { All, Hierarchy, Exact, None }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    // SuperAdmin: everything. Admin with no organization assigned: everything (treated like
    // SuperAdmin for organization scoping). Admin/Manager with an organization assigned: that
    // organization's subtree — the recursive hierarchy query naturally returns just the
    // organization itself when it has no children, so "parent sees children" / "child sees only
    // itself" fall out of the same query. Manager with no organization, or anyone below Manager:
    // exactly their own assigned organization, or nothing at all if they have none.
    private static (OrganizationScope Scope, int? OrganizationId) ResolveScope(IRequestContext context)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return (OrganizationScope.All, null);

        if (context.RoleLevel >= AdminRoleLevel)
            return context.OrganizationId.HasValue ? (OrganizationScope.Hierarchy, context.OrganizationId) : (OrganizationScope.All, null);

        if (context.RoleLevel >= ManagerRoleLevel)
            return context.OrganizationId.HasValue ? (OrganizationScope.Hierarchy, context.OrganizationId) : (OrganizationScope.None, null);

        return context.OrganizationId.HasValue ? (OrganizationScope.Exact, context.OrganizationId) : (OrganizationScope.None, null);
    }

    private static async Task<bool> CanManageAsync(IDbConnection connection, OrganizationScope scope, int? scopeOrganizationId, int targetOrganizationId, CancellationToken cancellationToken)
    {
        return scope switch
        {
            OrganizationScope.All => true,
            OrganizationScope.Exact => scopeOrganizationId == targetOrganizationId,
            OrganizationScope.Hierarchy => await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = scopeOrganizationId!.Value, TargetOrganizationId = targetOrganizationId },
                commandType: CommandType.StoredProcedure) == 1,
            _ => false
        };
    }

    public async Task<PagedResult<OrganizationDto>> GetOrganizationsAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        var (scope, scopeOrganizationId) = ResolveScope(context);
        if (scope == OrganizationScope.None)
            return new PagedResult<OrganizationDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = safePageNumber,
                PageSize = safePageSize
            };

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@RootOrganizationId", scope == OrganizationScope.Hierarchy ? scopeOrganizationId : (int?)null);
        p.Add("@ExactOrganizationId", scope == OrganizationScope.Exact ? scopeOrganizationId : (int?)null);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<OrganizationPageRow>(
            "sp_Organization_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<OrganizationDto>
        {
            Items = mapper.MapList<OrganizationDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<OrganizationDto?> GetOrganizationByTokenAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        // Supplier-scoped callers (real login or impersonated) never belong to an organization
        // hierarchy, so the normal scoping below would always resolve to None for them. A read-only
        // lookup by token is low-sensitivity (basic org info only) and is needed so a supplier can
        // resolve which organization to grant a contract price to (see ArticlePriceService).
        var (scope, scopeOrganizationId) = context.SupplierId.HasValue
            ? (OrganizationScope.All, (int?)null)
            : ResolveScope(context);
        if (scope == OrganizationScope.None)
            return null;

        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new
            {
                OrganizationToken = organizationToken,
                RootOrganizationId = scope == OrganizationScope.Hierarchy ? scopeOrganizationId : (int?)null,
                ExactOrganizationId = scope == OrganizationScope.Exact ? scopeOrganizationId : (int?)null
            },
            commandType: CommandType.StoredProcedure);

        return organization is null ? null : mapper.Map<OrganizationDto>(organization);
    }

    public async Task<bool> OrganizationExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_ExistsByName",
            new { NormalizedName = name.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }

    public async Task<OrganizationDto?> CreateOrganizationAsync(OrganizationDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        var (scope, scopeOrganizationId) = ResolveScope(context);
        if (scope == OrganizationScope.None)
            throw new ApiException(ErrorCodes.OrganizationCreateForbidden, "Not allowed to create organizations.", 403);

        await using var connection = connectionFactory.CreateConnection();

        // Creating a root organization (no parent) requires unrestricted scope; creating a child
        // requires the parent to be within the caller's manageable scope.
        var allowed = dto.ParentOrganizationId is null
            ? scope == OrganizationScope.All
            : await CanManageAsync(connection, scope, scopeOrganizationId, dto.ParentOrganizationId.Value, cancellationToken);

        if (!allowed)
            throw new ApiException(ErrorCodes.OrganizationParentOutsideScope, "Not allowed to create an organization under this parent.", 403);

        var created = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_Create",
            new
            {
                OrganizationToken = Guid.NewGuid(),
                Name = dto.Name,
                NormalizedName = dto.Name.ToUpperInvariant(),
                LegalName = dto.LegalName,
                Code = dto.Code,
                ParentOrganizationId = dto.ParentOrganizationId,
                OrganizationTypeId = dto.OrganizationTypeId == 0 ? (int?)null : dto.OrganizationTypeId,
                TimeZone = dto.TimeZone,
                CurrencyCode = dto.CurrencyCode,
                LanguageCode = dto.LanguageCode,
                IsActive = true,
                IsDeleted = false,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<OrganizationDto>(created);
    }

    public async Task<OrganizationDto?> EditOrganizationAsync(OrganizationDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = dto.OrganizationToken, RootOrganizationId = (int?)null, ExactOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        var (scope, scopeOrganizationId) = ResolveScope(context);

        if (!await CanManageAsync(connection, scope, scopeOrganizationId, existing.OrganizationId, cancellationToken))
            throw new ApiException(ErrorCodes.OrganizationOutsideScope, "Cannot edit an organization outside your scope.", 403);

        var newParentOrganizationId = dto.ParentOrganizationId ?? existing.ParentOrganizationId;

        if (newParentOrganizationId.HasValue && newParentOrganizationId != existing.ParentOrganizationId
            && !await CanManageAsync(connection, scope, scopeOrganizationId, newParentOrganizationId.Value, cancellationToken))
            throw new ApiException(ErrorCodes.OrganizationParentOutsideScope, "Cannot move an organization under a parent outside your scope.", 403);

        var newName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.Name;

        var updated = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_Update",
            new
            {
                OrganizationToken = dto.OrganizationToken,
                Name = newName,
                NormalizedName = newName.ToUpperInvariant(),
                LegalName = dto.LegalName ?? existing.LegalName,
                Code = dto.Code ?? existing.Code,
                ParentOrganizationId = newParentOrganizationId,
                OrganizationTypeId = dto.OrganizationTypeId == 0 ? (int?)null : dto.OrganizationTypeId,
                TimeZone = dto.TimeZone ?? existing.TimeZone,
                CurrencyCode = dto.CurrencyCode ?? existing.CurrencyCode,
                LanguageCode = dto.LanguageCode ?? existing.LanguageCode,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrganizationDto>(updated);
    }

    public async Task<bool> DeleteOrganizationAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null, ExactOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        var (scope, scopeOrganizationId) = ResolveScope(context);

        if (!await CanManageAsync(connection, scope, scopeOrganizationId, existing.OrganizationId, cancellationToken))
            throw new ApiException(ErrorCodes.OrganizationDeleteForbidden, "Not allowed to delete this organization.", 403);

        await connection.ExecuteAsync(
            "sp_Organization_SoftDelete",
            new
            {
                OrganizationToken = organizationToken,
                DeletedUtc = DateTime.UtcNow,
                DeletedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<BulkImportOrganizationResultDto> BulkImportOrganizationsAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken)
    {
        // Bulk import is gated as a flat admin-level capability, same philosophy as Users'/Suppliers'
        // bulk import, even though single-row CreateOrganizationAsync also allows a Manager with an
        // assigned OrganizationId to create a child org. Per-row calls to CreateOrganizationAsync
        // below still enforce the caller's own hierarchy scope via ResolveScope/CanManageAsync, so an
        // Admin scoped to a subtree can only bulk-create organizations inside that subtree — this gate
        // just raises the floor for who may use the bulk feature at all.
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.OrganizationBulkImportForbidden, "Only Admins and SuperAdmins can bulk-import organizations.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.OrganizationBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.First();

            // Skip the header row and any trailing blank rows (ClosedXML's used-range often
            // includes them from formatting bleed) so they don't count toward the row cap or
            // produce a spurious "missing required field" error.
            var dataRows = worksheet.RowsUsed()
                .Skip(1)
                .Where(row => row.CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                .ToList();

            if (dataRows.Count > MaxBulkImportRows)
                throw new ApiException(ErrorCodes.OrganizationBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportOrganizationResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            await using var connection = connectionFactory.CreateConnection();

            var parentCache = new Dictionary<string, Organization?>(StringComparer.OrdinalIgnoreCase);

            // IMPORTANT: rows are still processed strictly sequentially — same convention as every
            // other bulk import in this codebase — even though Organizations.NormalizedName IS
            // backed by a real unique filtered index (UX_Organizations_NormalizedName_NotDeleted,
            // unlike Users/Suppliers' non-unique indexes), so a duplicate name could never actually
            // commit twice even under parallelism. Sequential processing here is what lets a row's
            // ParentOrganizationName resolve against an organization created by an EARLIER row in
            // this same file (sp_Organization_GetByNormalizedName sees it because the prior row's
            // insert already committed) and keeps row-numbered error reporting deterministic.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();

                var name = row.Cell(1).GetString().Trim();
                var legalName = row.Cell(2).GetString().Trim();
                var code = row.Cell(3).GetString().Trim();
                var parentName = row.Cell(4).GetString().Trim();
                var timeZone = row.Cell(5).GetString().Trim();
                var currencyCode = row.Cell(6).GetString().Trim();
                var languageCode = row.Cell(7).GetString().Trim();

                var rowName = string.IsNullOrWhiteSpace(name) ? null : name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add(new BulkImportOrganizationRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.OrganizationBulkImportRowInvalid, Description = "Name is required." });
                    continue;
                }

                if (await OrganizationExistsByNameAsync(name, cancellationToken))
                {
                    result.Errors.Add(new BulkImportOrganizationRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.OrganizationAlreadyExists, Description = "An organization with this name already exists." });
                    continue;
                }

                int? parentOrganizationId = null;
                if (!string.IsNullOrWhiteSpace(parentName))
                {
                    var parentKey = parentName.ToUpperInvariant();
                    if (!parentCache.TryGetValue(parentKey, out var parent))
                    {
                        parent = await connection.QueryFirstOrDefaultAsync<Organization>(
                            "sp_Organization_GetByNormalizedName",
                            new { NormalizedName = parentKey },
                            commandType: CommandType.StoredProcedure);
                        parentCache[parentKey] = parent;
                    }

                    if (parent is null)
                    {
                        result.Errors.Add(new BulkImportOrganizationRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.OrganizationNotFound, Description = $"Parent organization '{parentName}' was not found." });
                        continue;
                    }

                    parentOrganizationId = parent.OrganizationId;
                    // A parent resolved from an earlier row in this same file won't be in that row's
                    // own cache entry yet, but the next row that references the SAME parent name will
                    // hit this cache instead of re-querying — cache is populated regardless of hit/miss.
                }

                string? resolvedCurrencyCode = null;
                if (!string.IsNullOrWhiteSpace(currencyCode))
                {
                    resolvedCurrencyCode = currencyCode.ToUpperInvariant();
                    if (!await currencyService.ExistsActiveByCodeAsync(resolvedCurrencyCode, cancellationToken))
                    {
                        result.Errors.Add(new BulkImportOrganizationRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.OrganizationInvalidCurrency, Description = $"Currency '{currencyCode}' is not a recognized, active currency." });
                        continue;
                    }
                }

                try
                {
                    var created = await CreateOrganizationAsync(
                        new OrganizationDto
                        {
                            Name = name,
                            LegalName = NullIfEmpty(legalName),
                            Code = NullIfEmpty(code),
                            ParentOrganizationId = parentOrganizationId,
                            TimeZone = NullIfEmpty(timeZone),
                            CurrencyCode = resolvedCurrencyCode,
                            LanguageCode = NullIfEmpty(languageCode)
                        },
                        context,
                        cancellationToken);

                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportOrganizationRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.OrganizationCreationFailed, Description = "Organization creation failed." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportOrganizationRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportOrganizationRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.OrganizationBulkImportRowFailed, Description = "An unexpected error occurred while creating this organization." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportOrganizationsAsync(string? searchField, string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.OrganizationBulkImportForbidden, "Only Admins and SuperAdmins can export organizations.", 403);

        var organizations = await GetOrganizationsAsync(1, MaxExportRows, searchField, searchText, includeInactive, context, cancellationToken);
        var organizationNames = organizations.Items.ToDictionary(o => o.OrganizationId, o => o.Name);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Organizations");

        string[] headers = ["Name", "LegalName", "Code", "ParentOrganizationName", "TimeZone", "CurrencyCode", "LanguageCode", "OrganizationTypeCode", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var organization in organizations.Items)
        {
            worksheet.Cell(r, 1).Value = organization.Name;
            worksheet.Cell(r, 2).Value = organization.LegalName;
            worksheet.Cell(r, 3).Value = organization.Code;
            worksheet.Cell(r, 4).Value = organization.ParentOrganizationId.HasValue
                ? organizationNames.GetValueOrDefault(organization.ParentOrganizationId.Value, "")
                : "";
            worksheet.Cell(r, 5).Value = organization.TimeZone;
            worksheet.Cell(r, 6).Value = organization.CurrencyCode;
            worksheet.Cell(r, 7).Value = organization.LanguageCode;
            worksheet.Cell(r, 8).Value = organization.OrganizationTypeCode;
            worksheet.Cell(r, 9).Value = organization.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"organizations_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateOrganizationImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.OrganizationBulkImportForbidden, "Only Admins and SuperAdmins can download the import template.", 403);

        // GetOrganizationsAsync's ResolveScope already scopes to the caller's own hierarchy, so the
        // reference sheet only ever lists organizations the caller could actually pick as a parent.
        var organizations = await GetOrganizationsAsync(1, MaxExportRows, null, null, false, context, cancellationToken);

        using var workbook = new XLWorkbook();

        var organizationsSheet = workbook.Worksheets.Add("Organizations");
        string[] headers = ["Name", "LegalName", "Code", "ParentOrganizationName", "TimeZone", "CurrencyCode", "LanguageCode"];
        for (var i = 0; i < headers.Length; i++)
            organizationsSheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        organizationsSheet.Row(1).Style.Font.Bold = true;

        var existingSheet = workbook.Worksheets.Add("Existing Organizations");
        existingSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Name", language);
        existingSheet.Row(1).Style.Font.Bold = true;
        var existingRow = 2;
        foreach (var organization in organizations.Items.OrderBy(o => o.Name))
            existingSheet.Cell(existingRow++, 1).Value = organization.Name;
        existingSheet.Columns().AdjustToContents();

        // Restricts the ParentOrganizationName column to a dropdown sourced from "Existing
        // Organizations" — this is what makes picking a parent foolproof for whoever fills the sheet
        // in Excel, and gives BulkImportOrganizationsAsync an unambiguous, already-existing name to
        // resolve via sp_Organization_GetByNormalizedName instead of fuzzy-matching free text. Uses a
        // workbook-scoped named range (rather than pointing the list validation straight at the other
        // sheet's range) because Excel's own UI refuses to let a user later edit a cross-sheet list
        // validation unless it goes through a defined name. Only wired up when there's at least one
        // existing organization — an empty named range would make the dropdown reject every value.
        if (organizations.Items.Count > 0)
        {
            var namedRange = workbook.DefinedNames.Add("ExistingOrganizationNames", existingSheet.Range(2, 1, existingRow - 1, 1));
            var parentColumnRange = organizationsSheet.Range(2, 4, MaxBulkImportRows + 1, 4);
            parentColumnRange.CreateDataValidation().List(namedRange.Name, true);
        }

        organizationsSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), "organizations_import_template.xlsx");
    }
}
