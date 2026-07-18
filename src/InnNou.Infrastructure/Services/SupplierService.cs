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
    // records. Creating a private supplier is open to Staff+ for their own organization (see
    // CreateSupplierAsync); creating a GLOBAL supplier, deleting a supplier, granting/revoking
    // system access, and changing an existing supplier's IsGlobal/owning-organization remain
    // superadmin-only — those are deliberately higher-trust operations.
    private const int StaffRoleLevel = 20;
    private const int AdminRoleLevel = 80;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    private const int MaxBulkImportRows = 500;
    private const int MaxExportRows = 10_000;

    private sealed class SupplierPageRow : Supplier { public int TotalCount { get; set; } }

    private sealed class PrivatizationImpact
    {
        public int ImpactedFavoriteOrganizationCount { get; set; }
        public int ImpactedDraftOrderOrganizationCount { get; set; }
        public int ImpactedTemplateOrganizationCount { get; set; }
        public int TotalImpactedOrganizationCount { get; set; }
    }

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
        // Below SuperAdmin, a caller needs either a SupplierId (sees its own supplier) or an
        // OrganizationId (sees globals + its own hierarchy's private suppliers) to see anything.
        if (context.RoleLevel < SuperAdminRoleLevel && !context.SupplierId.HasValue && !context.OrganizationId.HasValue)
            return new PagedResult<SupplierDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var isSuperAdmin = context.RoleLevel >= SuperAdminRoleLevel;
        var p = new DynamicParameters();
        p.Add("@ContextRoleLevel", context.RoleLevel);
        p.Add("@ContextSupplierId", isSuperAdmin ? (int?)null : context.SupplierId);
        p.Add("@ContextOrganizationId", isSuperAdmin ? (int?)null : context.OrganizationId);
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

    // Scope-aware uniqueness (see InnNou-Api CLAUDE.md, "Supplier global/private scoping"): a
    // global name must be unique among other globals; a private name must be unique among
    // globals UNION that exact owning organization's own private suppliers.
    // excludeSupplierId lets an edit re-check exclude the row being edited itself.
    public async Task<bool> SupplierExistsAsync(string name, bool isGlobal, int? organizationId, int? excludeSupplierId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_Supplier_ExistsByName",
            new
            {
                NormalizedName = name.ToUpperInvariant(),
                IsGlobal = isGlobal,
                OrganizationId = organizationId,
                ExcludeSupplierId = excludeSupplierId
            },
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

        if (context.RoleLevel >= SuperAdminRoleLevel)
            return mapper.Map<SupplierDto>(existing);

        if (context.SupplierId.HasValue)
            return context.SupplierId.Value == existing.SupplierId ? mapper.Map<SupplierDto>(existing) : null;

        if (existing.IsGlobal)
            return mapper.Map<SupplierDto>(existing);

        if (!context.OrganizationId.HasValue)
            return null;

        // Private supplier — visible if its owner is within the caller's own descending
        // hierarchy (caller's org itself, or any descendant) — same shape as
        // WarehouseService.CanManageReadAsync/OrderTemplateService.CanAccessTemplateAsync.
        // existing.OrganizationTokenResult only carries the token, not the internal id — resolve
        // the owner's internal id via the dedicated lookup instead.
        var currentOwner = await connection.QueryFirstOrDefaultAsync<OrganizationSupplier>(
            "sp_OrganizationSupplier_GetActiveBySupplierId",
            new { SupplierId = existing.SupplierId },
            commandType: CommandType.StoredProcedure);

        if (currentOwner is null)
            return null;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = currentOwner.OrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1 ? mapper.Map<SupplierDto>(existing) : null;
    }

    public async Task<SupplierDto?> CreateSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        var isGlobal = dto.IsGlobal ?? false;

        await using var connection = connectionFactory.CreateConnection();

        int? ownerOrganizationId;

        if (context.RoleLevel >= SuperAdminRoleLevel)
        {
            if (isGlobal)
            {
                ownerOrganizationId = null;
            }
            else
            {
                if (!dto.OrganizationToken.HasValue)
                    throw new ApiException(ErrorCodes.SupplierOrganizationTokenRequired, "An owning organization is required for a private supplier.", 400);

                var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                    "sp_Organization_GetByToken",
                    new { OrganizationToken = dto.OrganizationToken.Value, RootOrganizationId = (int?)null },
                    commandType: CommandType.StoredProcedure);

                if (organization is null)
                    throw new ApiException(ErrorCodes.SupplierOrganizationNotFound, "The specified owning organization was not found.", 404);

                ownerOrganizationId = organization.OrganizationId;
            }
        }
        else if (context.RoleLevel >= StaffRoleLevel && context.OrganizationId.HasValue)
        {
            if (isGlobal)
                throw new ApiException(ErrorCodes.SupplierCreateGlobalForbidden, "Only super admins can create a global supplier.", 403);

            // Server-resolved; any client-supplied OrganizationToken is ignored for this actor —
            // Staff+ can only ever create a private supplier scoped to their own organization.
            ownerOrganizationId = context.OrganizationId.Value;
        }
        else
        {
            throw new ApiException(ErrorCodes.SupplierCreateForbidden, "Insufficient permissions to create a supplier.", 403);
        }

        var duplicateExists = await SupplierExistsAsync(dto.Name, isGlobal, ownerOrganizationId, null, cancellationToken);
        if (duplicateExists)
            throw new ApiException(ErrorCodes.SupplierAlreadyExists, "A supplier with this name already exists.", 409);

        var hasAccess = dto.HasAccessToSystem ?? false;

        if (hasAccess && (string.IsNullOrWhiteSpace(dto.LoginEmail) || string.IsNullOrWhiteSpace(dto.Password)))
            throw new ApiException(ErrorCodes.SupplierLoginCredentialsRequired, "LoginEmail and Password are required when HasAccessToSystem is true.", 400);

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
                    IsGlobal = isGlobal,
                    SupplierType = dto.SupplierType ?? SupplierTypeCodes.Product,
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

            if (ownerOrganizationId.HasValue)
            {
                await connection.ExecuteAsync(
                    "sp_OrganizationSupplier_Assign",
                    new
                    {
                        OrganizationId = ownerOrganizationId.Value,
                        SupplierId = created.SupplierId,
                        CreatedUtc = DateTime.UtcNow,
                        CreatedBy = context.ActorUserToken.ToString()
                    },
                    transaction,
                    commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync(cancellationToken);

            if (ownerOrganizationId.HasValue)
            {
                var owner = await connection.QueryFirstOrDefaultAsync<OrganizationSupplier>(
                    "sp_OrganizationSupplier_GetActiveBySupplierId",
                    new { SupplierId = created.SupplierId },
                    commandType: CommandType.StoredProcedure);
                created.OrganizationTokenResult = owner?.OrganizationToken;
                created.OrganizationName = owner?.OrganizationName;
            }

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

        var currentOwner = await connection.QueryFirstOrDefaultAsync<OrganizationSupplier>(
            "sp_OrganizationSupplier_GetActiveBySupplierId",
            new { SupplierId = existing.SupplierId },
            commandType: CommandType.StoredProcedure);
        int? currentOwnerOrganizationId = currentOwner?.OrganizationId;

        var newIsGlobal = dto.IsGlobal ?? existing.IsGlobal;
        var wantsGlobalChange = dto.IsGlobal.HasValue && dto.IsGlobal.Value != existing.IsGlobal;
        // Only meaningful while staying/becoming private — a supplied OrganizationToken is
        // ignored once IsGlobal ends up true (there is no owner to assign).
        var wantsOwnerReassignment = !newIsGlobal && dto.OrganizationToken.HasValue;

        int? newOwnerOrganizationId = currentOwnerOrganizationId;

        if (wantsGlobalChange || wantsOwnerReassignment)
        {
            if (context.RoleLevel < SuperAdminRoleLevel)
                throw new ApiException(ErrorCodes.SupplierOwnershipChangeSuperadminOnly, "Only super admins can change a supplier's global/private status or owning organization.", 403);

            if (newIsGlobal)
            {
                newOwnerOrganizationId = null;
            }
            else
            {
                if (!dto.OrganizationToken.HasValue)
                    throw new ApiException(ErrorCodes.SupplierOrganizationTokenRequired, "An owning organization is required for a private supplier.", 400);

                var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
                    "sp_Organization_GetByToken",
                    new { OrganizationToken = dto.OrganizationToken.Value, RootOrganizationId = (int?)null },
                    commandType: CommandType.StoredProcedure);

                if (organization is null)
                    throw new ApiException(ErrorCodes.SupplierOrganizationNotFound, "The specified owning organization was not found.", 404);

                newOwnerOrganizationId = organization.OrganizationId;
            }

            // Privatization-impact confirmation — only for a genuine loss scenario
            // (Global->Private, or an A->B reassignment), never Private->Global, which only
            // ever adds visibility for everyone and never removes anyone's existing access.
            var isLossScenario = (existing.IsGlobal && !newIsGlobal)
                || (!existing.IsGlobal && !newIsGlobal && currentOwnerOrganizationId != newOwnerOrganizationId);

            if (isLossScenario && !dto.ConfirmPrivatizationImpact)
            {
                var impact = await connection.QueryFirstAsync<PrivatizationImpact>(
                    "sp_Supplier_GetPrivatizationImpact",
                    new { SupplierId = existing.SupplierId, NewOwnerOrganizationId = newOwnerOrganizationId },
                    commandType: CommandType.StoredProcedure);

                if (impact.TotalImpactedOrganizationCount > 0)
                    throw new ApiException(
                        ErrorCodes.SupplierPrivatizationImpact,
                        $"{impact.TotalImpactedOrganizationCount} other organization(s) would lose access to this supplier's articles: " +
                        $"{impact.ImpactedFavoriteOrganizationCount} with favorites, {impact.ImpactedDraftOrderOrganizationCount} with draft order lines, " +
                        $"{impact.ImpactedTemplateOrganizationCount} with template lines. Resubmit with confirmPrivatizationImpact=true to proceed anyway.",
                        409);
            }
        }
        else
        {
            // Ordinary-field edit, no ownership/global change. Deliberate read/write asymmetry
            // (see CLAUDE.md, "Supplier global/private scoping"): reads of a private supplier
            // cascade to the caller's entire descending hierarchy (a Super Asociado can SEE a
            // child Asociado's private supplier), but writes of ordinary fields require an
            // EXACT organization match — no hierarchy cascade. A Super Asociado can view but not
            // edit a child's private supplier's name/contact info; only SuperAdmin or that exact
            // organization's own Staff+ can. Do not "fix" this to match Warehouse/OrderTemplate's
            // symmetric read=write hierarchy scoping — it's intentional here.
            var isOwnSupplierLogin = context.SupplierId.HasValue && context.SupplierId.Value == existing.SupplierId;
            var isExactOwnerStaff = context.RoleLevel >= StaffRoleLevel
                && currentOwnerOrganizationId.HasValue
                && context.OrganizationId == currentOwnerOrganizationId;

            if (context.RoleLevel < SuperAdminRoleLevel && !isOwnSupplierLogin && !isExactOwnerStaff)
                throw new ApiException(ErrorCodes.SupplierOutsideScope, "Cannot edit another supplier.", 403);
        }

        var touchesAccess = dto.HasAccessToSystem.HasValue
            || !string.IsNullOrWhiteSpace(dto.LoginEmail)
            || !string.IsNullOrWhiteSpace(dto.Password);

        if (touchesAccess && context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.SupplierAccessSuperadminOnly, "Only super admins can change supplier system access.", 403);

        var newName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.Name;
        var newHasAccess = dto.HasAccessToSystem ?? existing.HasAccessToSystem;

        // Re-check uniqueness whenever anything that affects scope changes — closes a
        // pre-existing gap where this handler never checked uniqueness on edit at all.
        var nameChanging = !string.Equals(newName, existing.Name, StringComparison.Ordinal);
        if (nameChanging || wantsGlobalChange || wantsOwnerReassignment)
        {
            var duplicateExists = await SupplierExistsAsync(
                newName, newIsGlobal, newIsGlobal ? null : newOwnerOrganizationId, existing.SupplierId, cancellationToken);
            if (duplicateExists)
                throw new ApiException(ErrorCodes.SupplierAlreadyExists, "A supplier with this name already exists.", 409);
        }

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
            IsGlobal = newIsGlobal,
            SupplierType = dto.SupplierType ?? existing.SupplierType,
            HasAccessToSystem = newHasAccess,
            LastUpdatedUtc = DateTime.UtcNow,
            LastUpdatedBy = context.ActorUserToken.ToString()
        };

        var ownershipChanging = newIsGlobal != existing.IsGlobal || newOwnerOrganizationId != currentOwnerOrganizationId;

        Supplier? updated;

        if (touchesAccess || ownershipChanging)
        {
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                updated = await connection.QueryFirstOrDefaultAsync<Supplier>(
                    "sp_Supplier_Update", supplierUpdateParams, transaction, commandType: CommandType.StoredProcedure);

                if (touchesAccess)
                {
                    var shadowUser = await connection.QueryFirstOrDefaultAsync<User>(
                        "sp_User_GetBySupplierId",
                        new { SupplierId = existing.SupplierId },
                        transaction,
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
                            transaction,
                            commandType: CommandType.StoredProcedure);

                        if (emailExists == 1)
                            throw new ApiException(ErrorCodes.SupplierLoginEmailExists, "A user with this login email already exists.", 409);
                    }

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
                }

                if (ownershipChanging)
                {
                    if (newIsGlobal)
                    {
                        await connection.ExecuteAsync(
                            "sp_OrganizationSupplier_DeactivateAll",
                            new { SupplierId = existing.SupplierId, LastUpdatedUtc = DateTime.UtcNow, LastUpdatedBy = context.ActorUserToken.ToString() },
                            transaction,
                            commandType: CommandType.StoredProcedure);
                    }
                    else if (newOwnerOrganizationId.HasValue)
                    {
                        await connection.ExecuteAsync(
                            "sp_OrganizationSupplier_Assign",
                            new
                            {
                                OrganizationId = newOwnerOrganizationId.Value,
                                SupplierId = existing.SupplierId,
                                LastUpdatedUtc = DateTime.UtcNow,
                                LastUpdatedBy = context.ActorUserToken.ToString()
                            },
                            transaction,
                            commandType: CommandType.StoredProcedure);
                    }
                }

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

        if (updated is null)
            return null;

        var finalOwner = await connection.QueryFirstOrDefaultAsync<OrganizationSupplier>(
            "sp_OrganizationSupplier_GetActiveBySupplierId",
            new { SupplierId = updated.SupplierId },
            commandType: CommandType.StoredProcedure);
        updated.OrganizationTokenResult = finalOwner?.OrganizationToken;
        updated.OrganizationName = finalOwner?.OrganizationName;

        return mapper.Map<SupplierDto>(updated);
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
        // ultimately calls CreateSupplierAsync. Bulk import stays out of scope for the new
        // private-supplier ownership feature (no Owner Organization column) — a row whose IsGlobal
        // cell is FALSE will still reach CreateSupplierAsync, which now requires SuperAdmin to
        // supply an OrganizationToken for a private supplier; since bulk import never supplies
        // one, that row fails per-row with SUPPLIER_ORGANIZATION_TOKEN_REQUIRED rather than
        // silently creating an ownerless, effectively-invisible supplier.
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
                var supplierTypeText = row.Cell(13).GetString().Trim();

                var rowName = string.IsNullOrWhiteSpace(name) ? null : name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add(new BulkImportSupplierRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.SupplierBulkImportRowInvalid, Description = "Name is required." });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(supplierTypeText) && !SupplierTypeCodes.IsValid(supplierTypeText))
                {
                    result.Errors.Add(new BulkImportSupplierRowErrorDto { RowNumber = rowNumber, Name = rowName, Code = ErrorCodes.SupplierBulkImportRowInvalid, Description = "SupplierType must be Product, Service, or Mixed." });
                    continue;
                }

                if (await SupplierExistsAsync(name, true, null, null, cancellationToken))
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
                            SupplierType = string.IsNullOrWhiteSpace(supplierTypeText) ? SupplierTypeCodes.Product : supplierTypeText.Trim().ToUpperInvariant(),
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

        string[] headers = ["Name", "LegalName", "TaxId", "Email", "Phone", "AddressLine1", "AddressLine2", "City", "State", "PostalCode", "Country", "IsGlobal", "SupplierType", "Status"];
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
            worksheet.Cell(r, 13).Value = supplier.SupplierType;
            worksheet.Cell(r, 14).Value = supplier.IsActive ? "Active" : "Inactive";
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
        string[] headers = ["Name", "LegalName", "TaxId", "Email", "Phone", "AddressLine1", "AddressLine2", "City", "State", "PostalCode", "Country", "IsGlobal", "SupplierType"];
        for (var i = 0; i < headers.Length; i++)
            suppliersSheet.Cell(1, i + 1).Value = BulkExcelLocalization.Header(headers[i], language);
        suppliersSheet.Row(1).Style.Font.Bold = true;
        suppliersSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        return Task.FromResult((ms.ToArray(), "suppliers_import_template.xlsx"));
    }
}
