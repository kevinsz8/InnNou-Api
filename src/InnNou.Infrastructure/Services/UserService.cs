using ClosedXML.Excel;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Persistence;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Localization;
using InnNou.Shared.Mapping;
using System.Data;
using System.Text.RegularExpressions;

namespace InnNou.Infrastructure.Services;

public class UserService(IDbConnectionFactory connectionFactory, IMapper mapper, IRoleService roleService, IOrganizationService organizationService) : IUserService
{
    private sealed class UserPageRow : User { public int TotalCount { get; set; } }

    private const int AdminRoleLevel = 80;
    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private static bool IsPasswordStrong(string password) =>
        password.Length >= 8 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(c => !char.IsLetterOrDigit(c));

    public async Task<UserDto?> CreateUserAsync(UserDto userDto, IRequestContext context, CancellationToken cancellationToken)
    {
        if (userDto.OrganizationId.HasValue && userDto.SupplierId.HasValue)
            throw new ApiException(ErrorCodes.UserOrgAndSupplierConflict, "A user cannot belong to both an organization and a supplier", 400);

        await using var connection = connectionFactory.CreateConnection();

        var role = await connection.QueryFirstOrDefaultAsync<Role>(
            "sp_Role_GetById",
            new { RoleId = userDto.RoleId },
            commandType: CommandType.StoredProcedure);

        if (role is null)
            throw new ApiException(ErrorCodes.UserInvalidRole, "Invalid role", 400);

        if (role.RoleLevel > context.RoleLevel)
            throw new ApiException(ErrorCodes.UserCannotAssignHigherRole, "Cannot assign higher role", 403);

        if (context.RoleLevel < 100)
        {
            if (userDto.SupplierId.HasValue)
                throw new ApiException(ErrorCodes.UserSupplierCreateSuperadminOnly, "Only superadmin can create supplier users", 403);

            if (!context.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserInvalidOrganizationContext, "Invalid organization context", 400);

            if (!userDto.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserInvalidOrganizationAssignment, "Invalid organization assignment", 400);

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = userDto.OrganizationId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new ApiException(ErrorCodes.UserInvalidOrganizationAssignment, "Invalid organization assignment", 400);
        }

        var createdUser = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_Create",
            new
            {
                UserToken = Guid.NewGuid(),
                userDto.FirstName,
                userDto.LastName,
                Email = userDto.Email,
                NormalizedEmail = userDto.Email.ToUpperInvariant(),
                UserName = userDto.UserName,
                NormalizedUserName = userDto.UserName.ToUpperInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                userDto.RoleId,
                userDto.OrganizationId,
                userDto.SupplierId,
                IsActive = true,
                IsDeleted = false,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return createdUser is null ? null : mapper.Map<UserDto>(createdUser);
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(
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

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@ContextRoleLevel", context.RoleLevel);
        p.Add("@RootOrganizationId", context.RoleLevel >= 100 ? (int?)null : context.OrganizationId);
        p.Add("@SupplierId", context.RoleLevel < 100 && context.SupplierId.HasValue ? context.SupplierId : (int?)null);
        p.Add("@SearchField", string.IsNullOrWhiteSpace(searchField) ? null : searchField.Trim().ToLower());
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<UserPageRow>(
            "sp_User_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<UserDto>
        {
            Items = mapper.MapList<UserDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<UserDto?> EditUserAsync(UserDto request, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken",
            new { UserToken = request.UserToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (existing.RoleLevel > context.RoleLevel)
            throw new ApiException(ErrorCodes.UserCannotEditHigherRole, "Cannot edit higher role", 403);

        if (context.RoleLevel < 100 && context.OrganizationId.HasValue)
        {
            if (!existing.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot edit user from another organization", 403);

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = existing.OrganizationId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot edit user from another organization", 403);
        }

        var newRoleId = existing.RoleId;
        if (request.RoleId != 0 && request.RoleId != existing.RoleId)
        {
            var newRole = await connection.QueryFirstOrDefaultAsync<Role>(
                "sp_Role_GetById",
                new { RoleId = request.RoleId },
                commandType: CommandType.StoredProcedure);

            if (newRole is null)
                throw new ApiException(ErrorCodes.UserInvalidRole, "Invalid role", 400);

            if (newRole.RoleLevel > context.RoleLevel)
                throw new ApiException(ErrorCodes.UserCannotAssignHigherRole, "Cannot assign higher role", 403);

            newRoleId = newRole.RoleId;
        }

        var newEmail = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : existing.Email;
        var newUserName = !string.IsNullOrWhiteSpace(request.UserName) ? request.UserName : existing.UserName;

        var updatedUser = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_Update",
            new
            {
                UserToken = request.UserToken,
                Email = newEmail,
                NormalizedEmail = newEmail.ToUpperInvariant(),
                FirstName = !string.IsNullOrWhiteSpace(request.FirstName) ? request.FirstName : existing.FirstName,
                LastName = !string.IsNullOrWhiteSpace(request.LastName) ? request.LastName : existing.LastName,
                UserName = newUserName,
                NormalizedUserName = newUserName.ToUpperInvariant(),
                PasswordHash = !string.IsNullOrWhiteSpace(request.Password)
                    ? BCrypt.Net.BCrypt.HashPassword(request.Password)
                    : existing.PasswordHash,
                RoleId = newRoleId,
                OrganizationId = existing.OrganizationId,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updatedUser is null ? null : mapper.Map<UserDto>(updatedUser);
    }

    public async Task<bool> DeleteUserAsync(Guid userToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken",
            new { UserToken = userToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        if (existing.RoleLevel > context.RoleLevel)
            throw new ApiException(ErrorCodes.UserCannotDeleteHigherRole, "Cannot delete higher role", 403);

        if (context.RoleLevel < 100 && context.OrganizationId.HasValue)
        {
            if (!existing.OrganizationId.HasValue)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot delete user from another organization", 403);

            var canAccess = await connection.ExecuteScalarAsync<int>(
                "sp_Organization_IsInHierarchy",
                new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = existing.OrganizationId.Value },
                commandType: CommandType.StoredProcedure);

            if (canAccess != 1)
                throw new ApiException(ErrorCodes.UserOutsideOrganization, "Cannot delete user from another organization", 403);
        }

        var now = DateTime.UtcNow;
        var actor = context.ActorUserToken.ToString();

        await connection.ExecuteAsync(
            "sp_User_SoftDelete",
            new
            {
                UserToken = userToken,
                IsDeleted = true,
                DeletedUtc = now,
                DeletedBy = actor,
                LastUpdatedUtc = now,
                LastUpdatedBy = actor
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }

    public async Task<UserDto?> GetUserByTokenAsync(Guid userToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken",
            new { UserToken = userToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null || existing.IsDeleted)
            return null;

        if (context.RoleLevel < 100)
        {
            if (context.SupplierId.HasValue)
            {
                if (existing.SupplierId != context.SupplierId)
                    return null;
            }
            else if (context.OrganizationId.HasValue)
            {
                if (!existing.OrganizationId.HasValue)
                    return null;

                var canAccess = await connection.ExecuteScalarAsync<int>(
                    "sp_Organization_IsInHierarchy",
                    new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = existing.OrganizationId.Value },
                    commandType: CommandType.StoredProcedure);

                if (canAccess != 1)
                    return null;
            }
        }

        return mapper.Map<UserDto>(existing);
    }

    public async Task<bool> IsUserExists(string email, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_User_ExistsByEmail",
            new { NormalizedEmail = email.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }

    public async Task<BulkImportResultDto> BulkImportUsersAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UserBulkImportForbidden, "Only Admins and SuperAdmins can bulk-import users.", 403);

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(fileBytes));
        }
        catch
        {
            throw new ApiException(ErrorCodes.UserBulkImportInvalidFile, "The uploaded file is not a valid Excel (.xlsx) file.", 400);
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
                throw new ApiException(ErrorCodes.UserBulkImportTooManyRows, $"A single import file cannot contain more than {MaxBulkImportRows} rows.", 400);

            var result = new BulkImportResultDto { TotalRows = dataRows.Count };

            if (dataRows.Count == 0)
                return result;

            await using var connection = connectionFactory.CreateConnection();

            var roleCache = new Dictionary<string, Role?>(StringComparer.OrdinalIgnoreCase);
            var organizationCache = new Dictionary<string, Organization?>(StringComparer.OrdinalIgnoreCase);

            // IMPORTANT: rows must be processed strictly sequentially — never Task.WhenAll/Parallel.ForEach
            // this loop. Users.NormalizedEmail only has a non-unique index; the only thing preventing two
            // rows in this same file from creating duplicate users with the same email is that each row's
            // IsUserExists check below runs after the previous row's insert has already committed.
            foreach (var row in dataRows)
            {
                var rowNumber = row.RowNumber();

                var firstName = row.Cell(1).GetString().Trim();
                var lastName = row.Cell(2).GetString().Trim();
                var email = row.Cell(3).GetString().Trim();
                var userName = row.Cell(4).GetString().Trim();
                var password = row.Cell(5).GetString();
                var roleName = row.Cell(6).GetString().Trim();
                var organizationName = row.Cell(7).GetString().Trim();

                var rowEmail = string.IsNullOrWhiteSpace(email) ? null : email;

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                    string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName) ||
                    string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(roleName))
                {
                    result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.UserBulkImportRowInvalid, Description = "FirstName, LastName, Email, UserName, Password and RoleName are required." });
                    continue;
                }

                if (!EmailRegex.IsMatch(email))
                {
                    result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.UserBulkImportRowInvalid, Description = "Invalid email format." });
                    continue;
                }

                if (!IsPasswordStrong(password))
                {
                    result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.UserBulkImportWeakPassword, Description = "Password must be at least 8 characters and include an uppercase letter, lowercase letter, number and special character." });
                    continue;
                }

                var roleKey = roleName.ToUpperInvariant();
                if (!roleCache.TryGetValue(roleKey, out var role))
                {
                    role = await connection.QueryFirstOrDefaultAsync<Role>(
                        "sp_Role_GetByNormalizedName",
                        new { NormalizedName = roleKey },
                        commandType: CommandType.StoredProcedure);
                    roleCache[roleKey] = role;
                }

                if (role is null)
                {
                    result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.RoleNotFound, Description = $"Role '{roleName}' was not found." });
                    continue;
                }

                Organization? organization = null;
                if (!string.IsNullOrWhiteSpace(organizationName))
                {
                    var organizationKey = organizationName.ToUpperInvariant();
                    if (!organizationCache.TryGetValue(organizationKey, out organization))
                    {
                        organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                            "sp_Organization_GetByNormalizedName",
                            new { NormalizedName = organizationKey },
                            commandType: CommandType.StoredProcedure);
                        organizationCache[organizationKey] = organization;
                    }

                    if (organization is null)
                    {
                        result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.OrganizationNotFound, Description = $"Organization '{organizationName}' was not found." });
                        continue;
                    }
                }

                if (await IsUserExists(email, cancellationToken))
                {
                    result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.UserAlreadyExists, Description = "A user with this email already exists." });
                    continue;
                }

                try
                {
                    var created = await CreateUserAsync(
                        new UserDto
                        {
                            Email = email,
                            Password = password,
                            FirstName = firstName,
                            LastName = lastName,
                            UserName = userName,
                            RoleId = role.RoleId,
                            OrganizationId = organization?.OrganizationId
                        },
                        context,
                        cancellationToken);

                    if (created is null)
                    {
                        result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.UserCreationFailed, Description = "User creation failed." });
                        continue;
                    }

                    result.SuccessCount++;
                }
                catch (ApiException ex)
                {
                    result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ex.Code, Description = ex.Message });
                }
                catch (Exception)
                {
                    result.Errors.Add(new BulkImportRowErrorDto { RowNumber = rowNumber, Email = rowEmail, Code = ErrorCodes.UserBulkImportRowFailed, Description = "An unexpected error occurred while creating this user." });
                }
            }

            result.FailureCount = result.Errors.Count;
            return result;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> ExportUsersAsync(string? searchField, string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UserBulkImportForbidden, "Only Admins and SuperAdmins can export users.", 403);

        var users = await GetUsersAsync(1, MaxExportRows, searchField, searchText, includeInactive, context, cancellationToken);
        var roles = await roleService.GetRolesAsync(1, MaxExportRows, null, null, true, context, cancellationToken);
        var organizations = await organizationService.GetOrganizationsAsync(1, MaxExportRows, null, null, true, context, cancellationToken);

        var roleNames = roles.Items.ToDictionary(r => r.RoleId, r => r.Name);
        var organizationNames = organizations.Items.ToDictionary(o => o.OrganizationId, o => o.Name);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Users");

        string[] headers = ["FirstName", "LastName", "Email", "UserName", "RoleName", "OrganizationName", "Status"];
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        worksheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var user in users.Items)
        {
            worksheet.Cell(r, 1).Value = user.FirstName;
            worksheet.Cell(r, 2).Value = user.LastName;
            worksheet.Cell(r, 3).Value = user.Email;
            worksheet.Cell(r, 4).Value = user.UserName;
            worksheet.Cell(r, 5).Value = roleNames.GetValueOrDefault(user.RoleId, "");
            worksheet.Cell(r, 6).Value = user.OrganizationId.HasValue ? organizationNames.GetValueOrDefault(user.OrganizationId.Value, "") : "";
            worksheet.Cell(r, 7).Value = user.IsActive ? "Active" : "Inactive";
            r++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), $"users_export_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<(byte[] FileBytes, string FileName)> GenerateUserImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < AdminRoleLevel)
            throw new ApiException(ErrorCodes.UserBulkImportForbidden, "Only Admins and SuperAdmins can download the import template.", 403);

        // sp_Role_GetPaged already filters RoleLevel <= @MaxLevel (the caller's own level), and
        // GetOrganizationsAsync's ResolveScope already scopes to the caller's own hierarchy — so
        // both reference sheets only ever list values the caller could actually assign.
        var roles = await roleService.GetRolesAsync(1, MaxExportRows, null, null, false, context, cancellationToken);
        var organizations = await organizationService.GetOrganizationsAsync(1, MaxExportRows, null, null, false, context, cancellationToken);

        using var workbook = new XLWorkbook();

        var usersSheet = workbook.Worksheets.Add("Users");
        string[] headers = ["FirstName", "LastName", "Email", "UserName", "Password", "RoleName", "OrganizationName"];
        for (var i = 0; i < headers.Length; i++)
            usersSheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        usersSheet.Row(1).Style.Font.Bold = true;
        usersSheet.Columns().AdjustToContents();

        var rolesSheet = workbook.Worksheets.Add("Roles");
        rolesSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Name", language);
        rolesSheet.Row(1).Style.Font.Bold = true;
        var roleRow = 2;
        foreach (var role in roles.Items.OrderByDescending(role => role.RoleLevel))
            rolesSheet.Cell(roleRow++, 1).Value = role.Name;
        rolesSheet.Columns().AdjustToContents();

        var organizationsSheet = workbook.Worksheets.Add("Organizations");
        organizationsSheet.Cell(1, 1).Value = BulkExcelLocalization.Header("Name", language);
        organizationsSheet.Row(1).Style.Font.Bold = true;
        var organizationRow = 2;
        foreach (var organization in organizations.Items.OrderBy(organization => organization.Name))
            organizationsSheet.Cell(organizationRow++, 1).Value = organization.Name;
        organizationsSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return (ms.ToArray(), "users_import_template.xlsx");
    }
}
