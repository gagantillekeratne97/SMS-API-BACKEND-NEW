using Dapper;
using ServvistaWebAppAPI.Models;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Classes
{
    public class UserRepository
    {
        private readonly string _connectionString; 
        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<MTBL_TECH_OFFICERS> GetByUserNameAsync(string techCode)
        {
            const string query = @"
            SELECT COM_ID, TECH_CODE, TECH_NAME, MOBILE_NO, EMAIL, AREA, CITY, IS_ACTIVE, PASSWORD_HASH
            FROM MTBL_TECH_OFFICERS WHERE TECH_CODE = @techcode";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return await connection.QuerySingleOrDefaultAsync<MTBL_TECH_OFFICERS>(query, new { techcode = techCode});
            }
        }
    }
}
