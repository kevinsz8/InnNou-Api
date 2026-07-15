namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class User
    {
        public int UserId { get; set; }
        public Guid UserToken { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string NormalizedEmail { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string NormalizedUserName { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public int RoleId { get; set; }
        public int? OrganizationId { get; set; }
        public int? SupplierId { get; set; }
        public int? WarehouseContactId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool EmailConfirmed { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LastLoginUtc { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? DeletedUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}
