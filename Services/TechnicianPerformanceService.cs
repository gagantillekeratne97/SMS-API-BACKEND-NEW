using Dapper;
using Microsoft.AspNetCore.Connections;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;
using System.Data;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Services
{
    public class TechnicianPerformanceService : ITechnicianPerformanceService
    {
        private readonly string _connectionString;
        public TechnicianPerformanceService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<TechnicianPerformanceModel> GetPerformanceAsync(string techCode)
        {
            const string sql = @"
            SELECT
                COUNT(*) AS TotalJobs,
                SUM(
                    CASE 
                        WHEN UPPER(LTRIM(RTRIM(JOB_STATUS))) = 'COMPLETE'
                        THEN 1
                        ELSE 0
                    END
                ) AS CompletedJobs
            FROM TBL_DAILY_JOBS
            WHERE TECH_CODE = @TechCode;
        ";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var result = await connection.QuerySingleAsync<(int TotalJobs, int CompletedJobs)>(
                sql,
                new { TechCode = techCode }
                );

                double percentage = result.TotalJobs == 0
                    ? 0
                    : Math.Round((double)result.CompletedJobs / result.TotalJobs * 100, 2);

                return new TechnicianPerformanceModel
                {
                    TotalJobs = result.TotalJobs,
                    CompletedJobs = result.CompletedJobs,
                    PerformancePercentage = percentage
                };
            }            
        }
    }
}
