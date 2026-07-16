namespace InnNou.Infrastructure.Models;

// Flat projection returned by sp_Auth_GetUserByEmail, sp_Auth_GetUserByToken, sp_User_GetByToken.
// SP must alias: r.Name AS RoleName, r.RoleLevel AS RoleLevel, r.CanImpersonate AS CanImpersonate.
internal sealed class UserWithRoleResult
{
    public int UserId { get; set; }
    public Guid UserToken { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public int RoleId { get; set; }
    public int? OrganizationId { get; set; }
    public int? SupplierId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }
    public string? LastUpdatedBy { get; set; }
    public string RoleName { get; set; } = default!;
    public int RoleLevel { get; set; }
    public bool CanImpersonate { get; set; }

    // Only populated by sp_Auth_GetUserBySupplierToken (joins Suppliers.Name); null otherwise.
    public string? SupplierName { get; set; }

    // Only populated by sp_Auth_GetUserByWarehouseContactToken (joins WarehouseContacts.ContactName); null otherwise.
    public string? WarehouseContactName { get; set; }

    // Only populated by sp_Auth_GetTopUserByOrganizationToken (joins Organizations.Name); null otherwise.
    public string? OrganizationName { get; set; }
}
