using Microsoft.Data.SqlClient;

namespace DanaCotech.Infrastructure.Data;

public sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public SqlConnection CreateConnection() => new(connectionString);
}
