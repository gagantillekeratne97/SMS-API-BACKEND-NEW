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
            //Jobs Variables 
            int completedJobsCount = 0;
            double jobPerformancePercentage = 0;
            int totalJobsCount = 0;
            int lastWeekAllJobs = 0;
            int lastWeekCompletedJobs = 0;

            //Service Variables 
            int completedServiceCount = 0;
            int lastWeekAllServicesCount = 0;
            double servicePerformancePercentage = 0;
            int totalServicesCount = 0;
            int lastWeekCompletedServices = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                //Job performance details
                string completedJobsQuery = @"
                SELECT COUNT(*) AS CompletedJobs
                FROM TBL_DAILY_JOBS 
                WHERE TECH_CODE = @techcode
                AND JOB_STATUS = 'COMPLETE'";
                completedJobsCount = await connection.ExecuteScalarAsync<int>(completedJobsQuery, new { techcode = techCode });

                string totalJobsQuery = @"
                SELECT COUNT(*) AS TotalJobs
                FROM TBL_DAILY_JOBS 
                WHERE TECH_CODE = @techcode";

                totalJobsCount = await connection.ExecuteScalarAsync<int>(totalJobsQuery, new { techcode = techCode });

                string lastWeekAllJobsQuery = @"
                SELECT COUNT(*) AS lastWeekAllJobs
                FROM TBL_DAILY_JOBS
                WHERE TECH_CODE = @techcode 
                  AND DJ_DATE >= DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()) - 1, 0)
                  AND DJ_DATE <  DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0);";

                lastWeekAllJobs = await connection.ExecuteScalarAsync<int>(lastWeekAllJobsQuery, new { techcode = techCode });

                jobPerformancePercentage = Math.Round((double)completedJobsCount / totalJobsCount * 100, 2);

                string lastWeekCompletedJobsQuery = @"
                SELECT COUNT(*) AS lastWeekCompletedJobs
                FROM TBL_DAILY_JOBS
                WHERE TECH_CODE = @techcode 
                  AND DJ_DATE >= DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()) - 1, 0)
                  AND DJ_DATE <  DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0)
                  AND JOB_STATUS = 'COMPLETE';";

                lastWeekCompletedJobs = await connection.ExecuteScalarAsync<int>(lastWeekCompletedJobsQuery, new { techcode = techCode });

                //Service performance details

                string completedServiceVisitsQuery = @"
                SELECT COUNT(*) AS completeRow
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                        ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                        ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                        ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                        ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                        ('EXPT_SV6', s.EXPT_SV6, s.SV6)
                ) v (VisitNo, ExpectedDate, ActualVisit)
                WHERE s.TECH_CODE = @techcode
                  AND v.ExpectedDate IS NOT NULL
                  AND v.ActualVisit IS NOT NULL;   -- COMPLETED";

                completedServiceCount = await connection.ExecuteScalarAsync<int>(completedServiceVisitsQuery, new { techcode = techCode });

                string totalServiceVisitsQuery = @"
                SELECT COUNT(*) AS totalRow
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                        ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                        ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                        ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                        ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                        ('EXPT_SV6', s.EXPT_SV6, s.SV6)
                ) v (VisitNo, ExpectedDate, ActualVisit)
                WHERE s.TECH_CODE = @techcode
                  AND v.ExpectedDate IS NOT NULL;
                ";

                totalServicesCount = await connection.ExecuteScalarAsync<int>(totalServiceVisitsQuery, new { techcode = techCode });

                string totalServiceVisitsLastWeekQuery = @"
                SELECT COUNT(*) AS totalRow
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                        ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                        ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                        ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                        ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                        ('EXPT_SV6', s.EXPT_SV6, s.SV6)
                ) v (VisitNo, ExpectedDate, ActualVisit)
                WHERE s.TECH_CODE = @techcode
                  AND v.ExpectedDate IS NOT NULL
                  AND v.ExpectedDate >= DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()) - 1, 0)
                  AND v.ExpectedDate <  DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0);
                ";

                lastWeekAllServicesCount = await connection.ExecuteScalarAsync<int>(totalServiceVisitsLastWeekQuery, new { techcode = techCode });

                string completedServiceVisitsLastWeekQuery = @"
                SELECT COUNT(*) AS completeRow
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                        ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                        ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                        ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                        ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                        ('EXPT_SV6', s.EXPT_SV6, s.SV6)
                ) v (VisitNo, ExpectedDate, ActualVisit)
                WHERE s.TECH_CODE = @techcode
                  AND v.ExpectedDate IS NOT NULL
                  AND v.ActualVisit IS NOT NULL
                  AND v.ExpectedDate >= DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()) - 1, 0)
                  AND v.ExpectedDate <  DATEADD(WEEK, DATEDIFF(WEEK, 0, GETDATE()), 0);
                ";

                lastWeekCompletedServices = await connection.ExecuteScalarAsync<int>(completedServiceVisitsLastWeekQuery, new { techcode = techCode });

                servicePerformancePercentage = Math.Round((double)completedServiceCount / totalServicesCount * 100, 2);
            }

            return new TechnicianPerformanceModel
            {
                CompletedJobs = completedJobsCount,
                jobPerformancePercentage = jobPerformancePercentage,
                TotalJobs = totalJobsCount,
                lastWeekAllJobs = lastWeekAllJobs, 
                lastWeekCompletedJobs = lastWeekCompletedJobs, 
                completedServices = completedServiceCount,
                totalServices = totalServicesCount,
                lastWeekAllServices = lastWeekAllServicesCount,
                lastWeekCompetedServices = lastWeekCompletedServices,
                servicesPerformancePercentage = servicePerformancePercentage 
            };
        }        
    }
}
