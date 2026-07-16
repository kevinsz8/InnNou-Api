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

public class WarehouseContactService(IDbConnectionFactory connectionFactory, IMapper mapper) : IWarehouseContactService
{
    // Shared "WAREHOUSE" role (added 2026-07-16, resolving a previously-deferred design
    // question — see CLAUDE.md, "Warehouse contacts (shadow User)"). A short-lived
    // dedicated "WAREHOUSE_CONTACT" role and a separate per-Warehouse shadow-user
    // impersonation feature both existed briefly the same day and were reversed —
    // impersonating anything warehouse-related always goes through a specific contact
    // now, so WAREHOUSE is the only warehouse-side role left, and RoleLevel was bumped
    // 10 -> 20 (it used to back a read-mostly warehouse-only identity) so contacts keep
    // full Staff-level capability when impersonated.
    private const string WarehouseContactRoleNormalizedName = "WAREHOUSE";
    private const string NoAccessEmailDomain = "@no-access.innou.internal";

    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;

    private sealed class WarehouseContactPageRow : WarehouseContact { public int TotalCount { get; set; } }

    // Same rule as WarehouseService.CanManageOrganizationAsync — deliberately duplicated
    // rather than shared, matching this codebase's convention of each service owning its
    // own authorization logic.
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (context.RoleLevel < StaffRoleLevel || !context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    private static async Task<bool> CanManageReadAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    public async Task<PagedResult<WarehouseContactDto>> GetPagedByWarehouseTokenAsync(
        Guid warehouseToken,
        int pageNumber,
        int pageSize,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);

        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = warehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null || !await CanManageReadAsync(connection, context, warehouse.OrganizationId))
            return new PagedResult<WarehouseContactDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var p = new DynamicParameters();
        p.Add("@WarehouseId", warehouse.WarehouseId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim());
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<WarehouseContactPageRow>(
            "sp_WarehouseContact_GetPagedByWarehouseId", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<WarehouseContactDto>
        {
            Items = mapper.MapList<WarehouseContactDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<WarehouseContactDto?> GetByTokenAsync(Guid warehouseContactToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var contact = await connection.QueryFirstOrDefaultAsync<WarehouseContact>(
            "sp_WarehouseContact_GetByToken", new { WarehouseContactToken = warehouseContactToken }, commandType: CommandType.StoredProcedure);

        if (contact is null || !await CanManageReadAsync(connection, context, contact.WarehouseOrganizationId!.Value))
            return null;

        return mapper.Map<WarehouseContactDto>(contact);
    }

    public async Task<WarehouseContactDto?> CreateAsync(WarehouseContactDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var warehouse = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "sp_Warehouse_GetByToken", new { WarehouseToken = dto.WarehouseToken }, commandType: CommandType.StoredProcedure);

        if (warehouse is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, warehouse.OrganizationId))
            throw new ApiException(ErrorCodes.WarehouseContactOutsideScope, "Cannot manage contacts for a warehouse outside your scope.", 403);

        var hasAccess = dto.HasAccessToSystem ?? false;

        if (hasAccess && context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.WarehouseContactAccessSuperadminOnly, "Only super admins can grant system access to a warehouse contact.", 403);

        if (hasAccess && (string.IsNullOrWhiteSpace(dto.LoginEmail) || string.IsNullOrWhiteSpace(dto.Password)))
            throw new ApiException(ErrorCodes.WarehouseContactLoginCredentialsRequired, "LoginEmail and Password are required when HasAccessToSystem is true.", 400);

        if (hasAccess)
        {
            var emailExists = await connection.ExecuteScalarAsync<int>(
                "sp_User_ExistsByEmail",
                new { NormalizedEmail = dto.LoginEmail!.ToUpperInvariant() },
                commandType: CommandType.StoredProcedure);

            if (emailExists == 1)
                throw new ApiException(ErrorCodes.WarehouseContactLoginEmailExists, "A user with this login email already exists.", 409);
        }

        var contactRole = await connection.QueryFirstOrDefaultAsync<Role>(
            "sp_Role_GetByNormalizedName",
            new { NormalizedName = WarehouseContactRoleNormalizedName },
            commandType: CommandType.StoredProcedure);

        if (contactRole is null)
            throw new InvalidOperationException("Employee role is not configured.");

        var contactToken = Guid.NewGuid();

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await connection.QueryFirstOrDefaultAsync<WarehouseContact>(
                "sp_WarehouseContact_Create",
                new
                {
                    WarehouseContactToken = contactToken,
                    WarehouseId = warehouse.WarehouseId,
                    ContactName = dto.ContactName,
                    ContactType = dto.ContactType,
                    Department = dto.Department,
                    Phone = dto.Phone,
                    Mobile = dto.Mobile,
                    Fax = dto.Fax,
                    Email = dto.Email,
                    Notes = dto.Notes,
                    IsPrimary = dto.IsPrimary,
                    HasAccessToSystem = hasAccess,
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

            var loginEmail = hasAccess ? dto.LoginEmail! : $"warehousecontact-{contactToken:N}{NoAccessEmailDomain}";
            var userName = hasAccess ? dto.LoginEmail! : $"warehousecontact-{contactToken:N}";
            var password = hasAccess ? dto.Password! : Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            await connection.ExecuteAsync(
                "sp_User_Create",
                new
                {
                    UserToken = Guid.NewGuid(),
                    FirstName = dto.ContactName.Length > 150 ? dto.ContactName[..150] : dto.ContactName,
                    LastName = "(Warehouse Contact)",
                    Email = loginEmail,
                    NormalizedEmail = loginEmail.ToUpperInvariant(),
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = contactRole.RoleId,
                    OrganizationId = warehouse.OrganizationId,
                    SupplierId = (int?)null,
                    WarehouseContactId = created.WarehouseContactId,
                    IsActive = hasAccess,
                    IsDeleted = false,
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = context.ActorUserToken.ToString()
                },
                transaction,
                commandType: CommandType.StoredProcedure);

            await transaction.CommitAsync(cancellationToken);

            return mapper.Map<WarehouseContactDto>(created);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WarehouseContactDto?> EditAsync(WarehouseContactDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<WarehouseContact>(
            "sp_WarehouseContact_GetByToken", new { WarehouseContactToken = dto.WarehouseContactToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        var organizationId = existing.WarehouseOrganizationId!.Value;

        if (!await CanManageOrganizationAsync(connection, context, organizationId))
            throw new ApiException(ErrorCodes.WarehouseContactOutsideScope, "Cannot edit a contact for a warehouse outside your scope.", 403);

        var touchesAccess = dto.HasAccessToSystem.HasValue
            || !string.IsNullOrWhiteSpace(dto.LoginEmail)
            || !string.IsNullOrWhiteSpace(dto.Password);

        if (touchesAccess && context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.WarehouseContactAccessSuperadminOnly, "Only super admins can change a warehouse contact's system access.", 403);

        var newHasAccess = dto.HasAccessToSystem ?? existing.HasAccessToSystem;

        var contactUpdateParams = new
        {
            WarehouseContactToken = dto.WarehouseContactToken,
            ContactName = dto.ContactName,
            ContactType = dto.ContactType,
            Department = dto.Department,
            Phone = dto.Phone,
            Mobile = dto.Mobile,
            Fax = dto.Fax,
            Email = dto.Email,
            Notes = dto.Notes,
            IsPrimary = dto.IsPrimary,
            HasAccessToSystem = newHasAccess,
            LastUpdatedUtc = DateTime.UtcNow,
            LastUpdatedBy = context.ActorUserToken.ToString()
        };

        WarehouseContact? updated;

        if (touchesAccess)
        {
            var shadowUser = await connection.QueryFirstOrDefaultAsync<User>(
                "sp_User_GetByWarehouseContactId",
                new { WarehouseContactId = existing.WarehouseContactId },
                commandType: CommandType.StoredProcedure);

            if (shadowUser is null)
                throw new InvalidOperationException("Warehouse contact has no linked shadow user.");

            var isFirstActivation = newHasAccess && !existing.HasAccessToSystem
                && shadowUser.Email.EndsWith(NoAccessEmailDomain, StringComparison.OrdinalIgnoreCase);

            if (isFirstActivation && (string.IsNullOrWhiteSpace(dto.LoginEmail) || string.IsNullOrWhiteSpace(dto.Password)))
                throw new ApiException(ErrorCodes.WarehouseContactLoginCredentialsRequired, "LoginEmail and Password are required to grant system access for the first time.", 400);

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
                    throw new ApiException(ErrorCodes.WarehouseContactLoginEmailExists, "A user with this login email already exists.", 409);
            }

            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                updated = await connection.QueryFirstOrDefaultAsync<WarehouseContact>(
                    "sp_WarehouseContact_Update", contactUpdateParams, transaction, commandType: CommandType.StoredProcedure);

                await connection.ExecuteAsync(
                    "sp_User_SetWarehouseContactAccess",
                    new
                    {
                        WarehouseContactId = existing.WarehouseContactId,
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
            updated = await connection.QueryFirstOrDefaultAsync<WarehouseContact>(
                "sp_WarehouseContact_Update", contactUpdateParams, commandType: CommandType.StoredProcedure);
        }

        return updated is null ? null : mapper.Map<WarehouseContactDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid warehouseContactToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<WarehouseContact>(
            "sp_WarehouseContact_GetByToken", new { WarehouseContactToken = warehouseContactToken }, commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, existing.WarehouseOrganizationId!.Value))
            throw new ApiException(ErrorCodes.WarehouseContactOutsideScope, "Cannot delete a contact for a warehouse outside your scope.", 403);

        var now = DateTime.UtcNow;
        var actor = context.ActorUserToken.ToString();

        await connection.ExecuteAsync(
            "sp_WarehouseContact_SoftDelete",
            new
            {
                WarehouseContactToken = warehouseContactToken,
                DeletedUtc = now,
                DeletedBy = actor,
                LastUpdatedUtc = now,
                LastUpdatedBy = actor
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }
}
