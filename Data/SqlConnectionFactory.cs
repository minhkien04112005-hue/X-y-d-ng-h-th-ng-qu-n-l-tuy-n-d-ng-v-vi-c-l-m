using Microsoft.Data.SqlClient;

namespace RecruitmentAPI.Data
{
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Không tìm thấy connection string 'DefaultConnection' trong appsettings.json.");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
