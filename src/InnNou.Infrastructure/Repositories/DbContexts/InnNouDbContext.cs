using InnNou.Infrastructure.Repositories.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace InnNou.Infrastructure.Repositories.DbContexts
{
    public sealed class InnNouDbContext(DbContextOptions<InnNouDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Role> Roles { get; set; } = default!;
        public DbSet<UserRole> UserRoles { get; set; } = default!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tenant>().ToTable("Tenants");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
        }
    }
}