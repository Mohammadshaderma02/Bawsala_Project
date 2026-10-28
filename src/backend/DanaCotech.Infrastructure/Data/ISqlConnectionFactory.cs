using Microsoft.Data.SqlClient;

namespace DanaCotech.Infrastructure.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
