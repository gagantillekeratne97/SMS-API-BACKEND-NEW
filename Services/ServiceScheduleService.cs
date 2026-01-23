using Dapper;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ServvistaWebAppAPI.Services
{
    public class ServiceScheduleService : IServiceSchedule
    {
        private readonly string _connectionString; 
        public ServiceScheduleService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }
        private DateTime GetSriLankanTime()
        {
            string[] zoneInfo = { "Asia/Colombo", "Sri Lanka Standard Time" };
            foreach (var id in zoneInfo)
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
                }
                catch (Exception ex)
                {
                    return DateTime.UtcNow;
                }
            }

            throw new Exception("Sri Lankan timezone not found in this system.");
        }        

        public async Task<int> GetRemainingDays(string techCode, string machineRefNo)
        {
            int remainingDays = 0;
            List<ServiceVisitMonthlyInfo> machineInfo = new List<ServiceVisitMonthlyInfo>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                        SELECT
	                        MACHINE_REF AS machineRefNo,
                            MatchedColumn AS expectedVisitNo,
                            MatchedDate AS expectedVisitDate,
                            COUNT(DISTINCT MACHINE_REF) AS expectedVisitCount
                        FROM
                        (
                            SELECT
                                MACHINE_REF,
                                CONVERT(VARCHAR(10), 'EXPT_SV1') AS MatchedColumn,
                                CONVERT(CHAR(10), EXPT_SV1, 120) AS MatchedDate
                            FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                            WHERE TECH_CODE = @techcode
                              AND EXPT_SV1 >= @firstdaymonth AND EXPT_SV1 <= @lastdayofmonth

                            UNION ALL
                            SELECT
                                MACHINE_REF,
                                CONVERT(VARCHAR(10), 'EXPT_SV2'),
                                CONVERT(CHAR(10), EXPT_SV2, 120)
                            FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                            WHERE TECH_CODE = @techcode
                              AND EXPT_SV2 >= @firstdaymonth AND EXPT_SV2 <= @lastdayofmonth

                            UNION ALL
                            SELECT
                                MACHINE_REF,
                                CONVERT(VARCHAR(10), 'EXPT_SV3'),
                                CONVERT(CHAR(10), EXPT_SV3, 120)
                            FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                            WHERE TECH_CODE = @techcode
                              AND EXPT_SV3 >= @firstdaymonth AND EXPT_SV3 <= @lastdayofmonth

                            UNION ALL
                            SELECT
                                MACHINE_REF,
                                CONVERT(VARCHAR(10), 'EXPT_SV4'),
                                CONVERT(CHAR(10), EXPT_SV4, 120)
                            FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                            WHERE TECH_CODE = @techcode
                              AND EXPT_SV4 >= @firstdaymonth AND EXPT_SV4 <= @lastdayofmonth

                            UNION ALL
                            SELECT
                                MACHINE_REF,
                                CONVERT(VARCHAR(10), 'EXPT_SV5'),
                                CONVERT(CHAR(10), EXPT_SV5, 120)
                            FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                            WHERE TECH_CODE = @techcode
                              AND EXPT_SV5 >= @firstdaymonth AND EXPT_SV5 <= @lastdayofmonth

                            UNION ALL
                            SELECT
                                MACHINE_REF,
                                CONVERT(VARCHAR(10), 'EXPT_SV6'),
                                CONVERT(CHAR(10), EXPT_SV6, 120)
                            FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                            WHERE TECH_CODE = @techcode
                              AND EXPT_SV6 >= @firstdaymonth AND EXPT_SV6 <= @lastdayofmonth
                        ) x  
                        WHERE MACHINE_REF = @machinerefno
                        GROUP BY MatchedColumn, MatchedDate, MACHINE_REF
                        ORDER BY MatchedDate, MatchedColumn;
                        ";
                DateTime today = GetSriLankanTime(); 
                DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                DateTime lastDayOfMonth = new DateTime(today.Year, today.Month, 1).AddDays(31);
                var result = connection.QuerySingleOrDefault<ServiceVisitMonthlyInfo>(query, new { 
                    techcode = techCode, 
                    machinerefno = machineRefNo,                     
                    firstdaymonth = firstDayOfMonth, 
                    lastdayofmonth = lastDayOfMonth
                });
                DateTime dueDate = result.expectedVisitDate; 
                remainingDays = (dueDate - today).Days;                
            }
            return remainingDays; 
        }

        //Get Total Service visits of Technician 
        public async Task<List<ServiceVisitMonthlyInfo>> GetTotalServiceVisits(string techCode)
        {
            List<ServiceVisitMonthlyInfo> serviceSchedules = new List<ServiceVisitMonthlyInfo>(); 
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                                SELECT
                                    s.T_ID           AS rowId,
                                    s.CUS_ID         AS customerID,
                                    c.CUS_NAME       AS customerName,    
                                    c.CONTACT_PERSON AS contactPerson,
                                    c.TEL_NO         AS customerTelephone,
                                    s.M_LOC1         AS machineLocation01, 
                                    s.M_LOC2         AS machineLocation02, 
                                    s.M_LOC3         AS machineLocation03, 
                                    s.MACHINE_REF    AS machineRefNo,
                                    v.VisitNo        AS expectedVisitNo,
                                    CONVERT(char(10), v.ExpectedDate, 120) AS expectedVisitDate,
                                    CASE 
                                        WHEN v.ActualVisit IS NOT NULL THEN 'COMPLETED'
                                        ELSE 'PENDING'
                                    END AS VisitStatus
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
                                WHERE s.TECH_CODE = '0000'
                                  AND v.ExpectedDate IS NOT NULL
                                ORDER BY v.ExpectedDate, v.VisitNo;
                                ";                                

                var result = connection.Query<ServiceVisitMonthlyInfo>(query, new { techcode = techCode }).ToList();
                serviceSchedules = result;
            }

            return serviceSchedules;
        }

        //Get the previous visits for a machine ref 
        public async Task<List<PreviousServiceVisitModel>> GetPreviousServiceVisits(string techCode, string machineRefNo)
        {
            List<PreviousServiceVisitModel> previousServiceVisits = new List<PreviousServiceVisitModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                SELECT EXPT_SV1 AS exptsv1, EXPT_SV2 AS exptsv2, EXPT_SV3 AS exptsv3, EXPT_SV4 AS exptsv4, EXPT_SV5 AS exptsv5, EXPT_SV6 AS exptsv6,
                SV1, SV2, SV3, SV4, SV5, SV6 
                FROM TBL_SERVICE_SCEDULE_UPDATE
                WHERE TECH_CODE = @techcode AND MACHINE_REF = @machinerefno AND IS_ACTIVE = '0'";
                var result = connection.Query<PreviousServiceVisitModel>(query, new { 
                    techcode = techCode, 
                    machinerefno = machineRefNo 
                }).ToList();

                previousServiceVisits = result;
            }

            return previousServiceVisits;
        }

        //Update Service Schedule Visit 
        public async Task UpdateServiceSchedule(string techCode, int visitNo, string machineRefNo, string jobStatus)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string updateQuery = $@"
                UPDATE TBL_SERVICE_SCEDULE_UPDATE 
                SET SV{visitNo} = @visitdate, SV{visitNo}_STATUS = @jobstatus
                WHERE TECH_CODE = @techcode AND IS_ACTIVE = '1' AND MACHINE_REF = @machinerefno";
                DateTime visitDate = GetSriLankanTime(); 
                await connection.ExecuteAsync(updateQuery, new { 
                    techcode = techCode, 
                    machinerefno = machineRefNo,
                    visitdate = visitDate,
                    jobstatus = jobStatus
                });
            }
        }        

        //Get Monthly Machine Information 
        public async Task<List<ServiceVisitMonthlyInfo>> GetMonthlyVisits(string techCode)
        {
            List<ServiceVisitMonthlyInfo> machineCounts = new List<ServiceVisitMonthlyInfo>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                            SELECT
                                s.T_ID        AS rowId,
                                s.CUS_ID      AS customerID,
                                c.CUS_NAME    AS customerName,    
	                            c.CONTACT_PERSON AS contactPerson,
	                            c.TEL_NO	  AS customerTelephone,
	                            s.M_LOC1	  AS machineLocation01, 
	                            s.M_LOC2	  AS machineLocation02, 
	                            s.M_LOC3	  AS machineLocation03, 
                                s.MACHINE_REF AS machineRefNo,
                                v.VisitNo     AS expectedVisitNo,
                                CONVERT(char(10), v.ExpectedDate, 120) AS expectedVisitDate,
                                CASE 
                                    WHEN v.ActualVisit IS NOT NULL THEN 'COMPLETED'
                                    ELSE 'PENDING'
                                END AS VisitStatus
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
                              AND v.ExpectedDate BETWEEN @firstdaymonth AND @lastdayofmonth
                            ORDER BY v.ExpectedDate, v.VisitNo;";
                DateTime today = GetSriLankanTime();
                DateTime startDate = new DateTime(today.Year, today.Month, 1);
                DateTime endDate = startDate.AddMonths(1);
                var result = connection.Query<ServiceVisitMonthlyInfo>(query, new
                {
                    techcode = techCode,
                    firstdaymonth = startDate,
                    lastdayofmonth = endDate,
                });

                machineCounts = result.ToList();
            }

            return machineCounts;
        }        

        //Get Today Machine Information
        public async Task<List<ServiceVisitDailyInfoModel>> GetTodayServiceVisits(string techCode)
        {
            List<ServiceVisitDailyInfoModel> serviceSchedules = new List<ServiceVisitDailyInfoModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                SELECT
	                t.CUS_NAME AS cusName,
                    t.SERIAL_NO AS serialNo,
                    t.MACHINE_REF AS machineRefNo,
                    v.MatchedColumn AS expectedVisitNo,
                    v.MatchedDate AS expectedVisitDate
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE t
                CROSS APPLY
                (
                    SELECT 'EXPT_SV1', t.EXPT_SV1 WHERE t.EXPT_SV1 >= @FromDate AND t.EXPT_SV1 <= @ToDate
                    UNION ALL
                    SELECT 'EXPT_SV2', t.EXPT_SV2 WHERE t.EXPT_SV2 >= @FromDate AND t.EXPT_SV2 <= @ToDate
                    UNION ALL
                    SELECT 'EXPT_SV3', t.EXPT_SV3 WHERE t.EXPT_SV3 >= @FromDate AND t.EXPT_SV3 <= @ToDate
                    UNION ALL
                    SELECT 'EXPT_SV4', t.EXPT_SV4 WHERE t.EXPT_SV4 >= @FromDate AND t.EXPT_SV4 <= @ToDate
                    UNION ALL
                    SELECT 'EXPT_SV5', t.EXPT_SV5 WHERE t.EXPT_SV5 >= @FromDate AND t.EXPT_SV5 <= @ToDate
                    UNION ALL
                    SELECT 'EXPT_SV6', t.EXPT_SV6 WHERE t.EXPT_SV6 >= @FromDate AND t.EXPT_SV6 <= @ToDate
                ) v(MatchedColumn, MatchedDate)
                WHERE t.TECH_CODE = @techcode 
                AND IS_ACTIVE = '1';
                ";
                DateTime yesterdayDate = GetSriLankanTime().AddDays(-1);
                DateTime tomorrowDate = GetSriLankanTime().AddDays(1);
                List<ServiceVisitDailyInfoModel> result = connection.Query<ServiceVisitDailyInfoModel>(query, new { 
                    techcode = techCode, 
                    FromDate = yesterdayDate, 
                    ToDate = tomorrowDate
                }).ToList();                

                return result;
            }
        }
    }
}
