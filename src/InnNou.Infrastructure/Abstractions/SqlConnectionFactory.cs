using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace InnNou.Infrastructure.Abstractions;

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}
