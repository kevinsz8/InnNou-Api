using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace InnNou.Infrastructure.Repositories.DbContexts
{
    public class InnNouDbContextFactory : IDesignTimeDbContextFactory<InnNouDbContext>
    {
        public InnNouDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<InnNouDbContext>();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "InnNou.API"))
                .AddJsonFile("appsettings.json")
                .Build();
            var connectionString = configuration.GetConnectionString("InnNouConnection");

            optionsBuilder.UseSqlServer(connectionString);
            return new InnNouDbContext(optionsBuilder.Options);
        }
    }
}
