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

    private sealed class SupplierPageRow : Supplier { public int TotalCount { get; set; } }

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
            throw new UnauthorizedAccessException("Only super admins can create suppliers.");

        var hasAccess = dto.HasAccessToSystem ?? false;

        if (hasAccess && (string.IsNullOrWhiteSpace(dto.LoginEmail) || string.IsNullOrWhiteSpace(dto.Password)))
            throw new InvalidOperationException("LoginEmail and Password are required when HasAccessToSystem is true.");

        await using var connection = connectionFactory.CreateConnection();

        if (hasAccess)
        {
            var emailExists = await connection.ExecuteScalarAsync<int>(
                "sp_User_ExistsByEmail",
                new { NormalizedEmail = dto.LoginEmail!.ToUpperInvariant() },
                commandType: CommandType.StoredProcedure);

            if (emailExists == 1)
                throw new InvalidOperationException("A user with this login email already exists.");
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
            throw new UnauthorizedAccessException("Cannot edit another supplier.");

        var touchesAccess = dto.HasAccessToSystem.HasValue
            || !string.IsNullOrWhiteSpace(dto.LoginEmail)
            || !string.IsNullOrWhiteSpace(dto.Password);

        if (touchesAccess && context.RoleLevel < SuperAdminRoleLevel)
            throw new UnauthorizedAccessException("Only super admins can change supplier system access.");

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
                throw new InvalidOperationException("LoginEmail and Password are required to grant system access for the first time.");

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
                    throw new InvalidOperationException("A user with this login email already exists.");
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
            throw new UnauthorizedAccessException("Only super admins can delete suppliers.");

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
}
