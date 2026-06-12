using System.Data.Common;

namespace InnNou.Infrastructure.Abstractions;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
