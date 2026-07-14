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

public class SupplierService(IDbConnectionFactory connectionFactory, IMapper mapper) : ISupplierService
{
    private const string SupplierRoleNormalizedName = "SUPPLIER";
    private const string NoAccessEmailDomain = "@no-access.innou.internal";

    // Harmonized with ArticleService.AdminRoleLevel: Admin+ can browse/edit ordinary supplier
    // records. Creating/deleting a supplier and granting/revoking its system access remain
    // superadmin-only (RoleLevel >= 100) — those are deliberately higher-trust operations,
    // not just visibility/ordinary-field management.
    private const int AdminRoleLevel = 80;
    private const int SuperAdminRoleLevel = 100;

    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    private sealed class SupplierPageRow : Supplier { public int TotalCount { get; set; } }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool IsTruthy(string value) =>
        string.Equals(value.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value.Trim(), "YES", StringComparison.OrdinalIgnoreCase) ||
        value.Trim() == "1";

    public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel && !context.SupplierId.HasValue)
            return new PagedResult<SupplierDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@ContextRoleLevel", context.RoleLevel);
        p.Add("@ContextSupplierId", context.RoleLevel >= AdminRoleLevel ? (int?)null : context.SupplierId);
        p.Add("@SearchField", string.IsNullOrWhiteSpace(searchField) ? null : searchField.Trim().ToLower());
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<SupplierPageRow>(
            "sp_Supplier_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<SupplierDto>
        {
            Items = mapper.MapList<SupplierDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<bool> SupplierExistsAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_Supplier_ExistsByName",
            new { NormalizedName = name.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }

    public async Task<SupplierDto?> GetSupplierByTokenAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken",
            new { SupplierToken = supplierToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (context.RoleLevel < AdminRoleLevel && context.SupplierId != existing.SupplierId)
            return null;

        return mapper.Map<SupplierDto>(existing);
    }

    public async Task<SupplierDto?> CreateSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.SupplierCreateSuperadminOnly, "Only super admins can create suppliers.", 403);

        var hasAccess = dto.HasAccessToSystem ?? false;

        if (hasAccess && (string.IsNullOrWhiteSpace(dto.LoginEmail) || string.IsNullOrWhiteSpace(dto.Password)))
            throw new ApiException(ErrorCodes.SupplierLoginCredentialsRequired, "LoginEmail and Password are required when HasAccessToSystem is true.", 400);

        await using var connection = connectionFactory.CreateConnection();

        if (hasAccess)
        {
            var emailExists = await connection.ExecuteScalarAsync<int>(
                "sp_User_ExistsByEmail",
                new { NormalizedEmail = dto.LoginEmail!.ToUpperInvariant() },
                commandType: CommandType.StoredProcedure);

            if (emailExists == 1)
                throw new ApiException(ErrorCodes.SupplierLoginEmailExists, "A user with this login email already exists.", 409);
        }

        var supplierRole = await connection.QueryFirstOrDefaultAsync<Role>(
            "sp_Role_GetByNormalizedName",
            new { NormalizedName = SupplierRoleNormalizedName },
            commandType: CommandType.StoredProcedure);

        if (supplierRole is null)
            throw new InvalidOperationException("Supplier role is not configured.");

        var supplierToken = Guid.NewGuid();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await connection.QueryFirstOrDefaultAsync<Supplier>(
                "sp_Supplier_Create",
                new
                {
                    SupplierToken = supplierToken,
                    Name = dto.Name,
                    NormalizedName = dto.Name.ToUpperInvariant(),
                    LegalName = dto.LegalName,
                    TaxId = dto.TaxId,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    City = dto.City,
                    State = dto.State,
                    PostalCode = dto.PostalCode,
                    Country = dto.Country,
                    IsGlobal = dto.IsGlobal ?? false,
                    HasAccessToSystem = hasAccess,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = context.ActorUserToken.ToString()
                },
                transaction,
                commandType: CommandType.StoredProcedure);

            if (created is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var loginEmail = hasAccess ? dto.LoginEmail! : $"supplier-{supplierToken:N}{NoAccessEmailDomain}";
            var userName = hasAccess ? dto.LoginEmail! : $"supplier-{supplierToken:N}";
            var password = hasAccess ? dto.Password! : Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            await connection.ExecuteAsync(
                "sp_User_Create",
                new
                {
                    UserToken = Guid.NewGuid(),
                    FirstName = dto.Name.Length > 150 ? dto.Name[..150] : dto.Name,
                    LastName = "(Supplier Account)",
                    Email = loginEmail,
                    NormalizedEmail = loginEmail.ToUpperInvariant(),
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = supplierRole.RoleId,
                    OrganizationId = (int?)null,
                    SupplierId = created.SupplierId,
                    IsActive = hasAccess,
                    IsDeleted = false,
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = context.ActorUserToken.ToString()
                },
                transaction,
                commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            return mapper.Map<SupplierDto>(created);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SupplierDto?> EditSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken",
            new { SupplierToken = dto.SupplierToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (context.RoleLevel < AdminRoleLevel && context.SupplierId != existing.SupplierId)
            throw new ApiException(ErrorCodes.SupplierOutsideScope, "Cannot edit another supplier.", 403);

        var touchesAccess = dto.HasAccessToSystem.HasValue
            || !string.IsNullOrWhiteSpace(dto.LoginEmail)
            || !string.IsNullOrWhiteSpace(dto.Password);

        if (touchesAccess && context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.SupplierAccessSuperadminOnly, "Only super admins can change supplier system access.", 403);

        var newName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.Name;
        var newHasAccess = dto.HasAccessToSystem ?? existing.HasAccessToSystem;

        var supplierUpdateParams = new
        {
            SupplierToken = dto.SupplierToken,
            Name = newName,
            NormalizedName = newName.ToUpperInvariant(),
            LegalName = dto.LegalName ?? existing.LegalName,
            TaxId = dto.TaxId ?? existing.TaxId,
            Email = dto.Email ?? existing.Email,
            Phone = dto.Phone ?? existing.Phone,
            AddressLine1 = dto.AddressLine1 ?? existing.AddressLine1,
            AddressLine2 = dto.AddressLine2 ?? existing.AddressLine2,
            City = dto.City ?? existing.City,
            State = dto.State ?? existing.State,
            PostalCode = dto.PostalCode ?? existing.PostalCode,
            Country = dto.Country ?? existing.Country,
            IsGlobal = dto.IsGlobal ?? existing.IsGlobal,
            HasAccessToSystem = newHasAccess,
            LastUpdatedUtc = DateTime.UtcNow,
            LastUpdatedBy = context.ActorUserToken.ToString()
        };

        Supplier? updated;

        if (touchesAccess)
        {
            var shadowUser = await connection.QueryFirstOrDefaultAsync<User>(
                "sp_User_GetBySupplierId",
                new { SupplierId = existing.SupplierId },
                commandType: CommandType.StoredProcedure);

            if (shadowUser is null)
                throw new InvalidOperationException("Supplier has no linked shadow user.");

            var isFirstActivation = newHasAccess && !existing.HasAccessToSystem
                && shadowUser.Email.EndsWith(NoAccessEmailDomain, StringComparison.OrdinalIgnoreCase);

            if (isFirstActivation && (string.IsNullOrWhiteSpace(dto.LoginEmail) || string.IsNullOrWhiteSpace(dto.Password)))
                throw new ApiException(ErrorCodes.SupplierLoginCredentialsRequired, "LoginEmail and Password are required to grant system access for the first time.", 400);

            var newLoginEmail = !string.IsNullOrWhiteSpace(dto.LoginEmail) ? dto.LoginEmail! : shadowUser.Email;
            var newUserName = !string.IsNullOrWhiteSpace(dto.LoginEmail) ? dto.LoginEmail! : shadowUser.UserName;
            var newPasswordHash = !string.IsNullOrWhiteSpace(dto.Password)
                ? BCrypt.Net.BCrypt.HashPassword(dto.Password)
                : shadowUser.PasswordHash;

            if (!string.IsNullOrWhiteSpace(dto.LoginEmail)
                && !string.Equals(dto.LoginEmail, shadowUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await connection.ExecuteScalarAsync<int>(
                    "sp_User_ExistsByEmail",
                    new { NormalizedEmail = dto.LoginEmail!.ToUpperInvariant() },
                    commandType: CommandType.StoredProcedure);

                if (emailExists == 1)
                    throw new ApiException(ErrorCodes.SupplierLoginEmailExists, "A user with this login email already exists.", 409);
            }

            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                updated = await connection.QueryFirstOrDefaultAsync<Supplier>(
                    "sp_Supplier_Update", supplierUpdateParams, transaction, commandType: CommandType.StoredProcedure);

                await connection.ExecuteAsync(
                    "sp_User_SetSupplierAccess",
                    new
                    {
                        SupplierId = existing.SupplierId,
                        Email = newLoginEmail,
                        NormalizedEmail = newLoginEmail.ToUpperInvariant(),
                        UserName = newUserName,
                        NormalizedUserName = newUserName.ToUpperInvariant(),
                        PasswordHash = newPasswordHash,
                        IsActive = newHasAccess,
                        LastUpdatedUtc = DateTime.UtcNow,
                        LastUpdatedBy = context.ActorUserToken.ToString()
                    },
                    transaction,
                    commandType: CommandType.StoredProcedure);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            updated = await connection.QueryFirstOrDefaultAsync<Supplier>(
                "sp_Supplier_Update", supplierUpdateParams, commandType: CommandType.StoredProcedure);
        }

        return updated is null ? null : mapper.Map<SupplierDto>(updated);
    }

    public async Task<bool> DeleteSupplierAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.SupplierDeleteSuperadminOnly, "Only super admins can delete suppliers.", 403);

        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken",
            new { SupplierToken = supplierToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        await connection.ExecuteAsync(
            "sp_Supplier_SoftDelete",
            new
            {
                SupplierToken = supplierToken,
                IsDeleted = true,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<BulkImportSupplierResultDto> BulkImportSuppliersAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken)
    {
        // Gated at SuperAdminRoleLevel (not AdminRoleLevel like Users' bulk import) because each row
        // ultimately calls CreateSupplierAsync, which itself is superadmin-only — gating lower here
        // would let an Admin upload a file where every single row fails with SUPPLIER_CREATE_SUPERADMIN_ONLY.
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.SupplierBulkImportForbidden, "Only super admins can bulk-import suppliers.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.SupplierBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
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
                throw new ApiException(ErrorCodes.SupplierBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportSupplierResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            // IMPORTANT: rows must be processed strictly sequentially — never Task.WhenAll/Parallel.ForEach
            // this loop. Suppliers.NormalizedName only has a non-unique index (UX_Suppliers_NormalizedName_NotDeleted);
            // the only thing preventing two rows in this same file from creating duplicate suppliers with the
            // same name is that each row's SupplierExistsAsync check below runs after the previous row's
            // insert has already committed.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();

                var name = row.Cell(1).GetString().Trim();
                var legalName = row.Cell(2).GetString().Trim();
                var taxId = row.Cell(3).GetString().Trim();
                var email = row.Cell(4).GetString().Trim();
                var phone = row.Cell(5).GetString().Trim();
                var addressLine1 = row.Cell(6).GetString().Trim();
                var addressLine2 = row.Cell(7).GetString().Trim();
                var city = row.Cell(8).GetString().Trim();
                var state = row.Cell(9).GetString().Trim();
                var postalCode = row.Cell(10).GetString().Trim();
                var country = row.Cell(11).GetString().Trim();
                var isGlobalText = row.Cell(12).GetString();

                var rowName = string.IsNullOrWhiteSpace(name) ? null : name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add(new BulkImportSupplierRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.SupplierBulkImportRowInvalid, Description = "Name is required." });
                    continue;
                }

                if (await SupplierExistsAsync(name, cancellationToken))
                {
                    result.Errors.Add(new BulkImportSupplierRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.SupplierAlreadyExists, Description = "A supplier with this name already exists." });
                    continue;
                }

                try
                {
                    // HasAccessToSystem/LoginEmail/Password are deliberately not sourced from the
                    // import file — bulk-imported suppliers always start with no system access;
                    // granting it later is a separate, superadmin-only EditSupplierAsync call.
                    var created = await CreateSupplierAsync(
                        new SupplierDto
                        {
                            Name = name,
                            LegalName = NullIfEmpty(legalName),
                            TaxId = NullIfEmpty(taxId),
                            Email = NullIfEmpty(email),
                            Phone = NullIfEmpty(phone),
                            AddressLine1 = NullIfEmpty(addressLine1),
                            AddressLine2 = NullIfEmpty(addressLine2),
                            City = NullIfEmpty(city),
                            State = NullIfEmpty(state),
                            PostalCode = NullIfEmpty(postalCode),
                            Country = NullIfEmpty(country),
                            IsGlobal = IsTruthy(isGlobalText),
                            HasAccessToSystem = false
                        },
                        context,
                        cancellationToken);

                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportSupplierRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.SupplierCreationFailed, Description = "Supplier creation failed." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportSupplierRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportSupplierRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.SupplierBulkImportRowFailed, Description = "An unexpected error occurred while creating this supplier." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportSuppliersAsync(string? searchField, string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SupplierBulkImportForbidden, "Only Admins and SuperAdmins can export suppliers.", 403);

        var suppliers = await GetSuppliersAsync(1, MaxExportRows, searchField, searchText, includeInactive, context, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Suppliers");

        string[] headers = ["Name", "LegalName", "TaxId", "Email", "Phone", "AddressLine1", "AddressLine2", "City", "State", "PostalCode", "Country", "IsGlobal", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var supplier in suppliers.Items)
        {
            worksheet.Cell(r, 1).Value = supplier.Name;
            worksheet.Cell(r, 2).Value = supplier.LegalName;
            worksheet.Cell(r, 3).Value = supplier.TaxId;
            worksheet.Cell(r, 4).Value = supplier.Email;
            worksheet.Cell(r, 5).Value = supplier.Phone;
            worksheet.Cell(r, 6).Value = supplier.AddressLine1;
            worksheet.Cell(r, 7).Value = supplier.AddressLine2;
            worksheet.Cell(r, 8).Value = supplier.City;
            worksheet.Cell(r, 9).Value = supplier.State;
            worksheet.Cell(r, 10).Value = supplier.PostalCode;
            worksheet.Cell(r, 11).Value = supplier.Country;
            worksheet.Cell(r, 12).Value = (supplier.IsGlobal ?? false) ? "TRUE" : "FALSE";
            worksheet.Cell(r, 13).Value = supplier.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"suppliers_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public Task<(byte[] FileBytes, string FileName)> GenerateSupplierImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.SupplierBulkImportForbidden, "Only Admins and SuperAdmins can download the import template.", 403);

        // Unlike Users' template, Suppliers have no Role/Organization-style foreign key resolved
        // by name, so no reference lookup sheet is needed — just the data-entry headers.
        using var workbook = new XLWorkbook();

        var suppliersSheet = workbook.Worksheets.Add("Suppliers");
        string[] headers = ["Name", "LegalName", "TaxId", "Email", "Phone", "AddressLine1", "AddressLine2", "City", "State", "PostalCode", "Country", "IsGlobal"];
        for (var i = 0; i < headers.Length; i++)
            suppliersSheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        suppliersSheet.Row(1).Style.Font.Bold = true;
        suppliersSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return Task.FromResult((ms.ToArray(), "suppliers_import_template.xlsx"));
    }
}
