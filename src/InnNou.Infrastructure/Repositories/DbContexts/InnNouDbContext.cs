using InnNou.Infrastructure.Repositories.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace InnNou.Infrastructure.Repositories.DbContexts
{
    public sealed class InnNouDbContext(DbContextOptions<InnNouDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Role> Roles { get; set; } = default!;
        public DbSet<Hotel> Hotels { get; set; } = default!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<Hotel>().ToTable("Hotels");
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
        }
    }
}