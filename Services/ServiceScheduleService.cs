using Dapper;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                DateTime todayDate = GetSriLankanTime().Date;
                // Calculate last month date range
                DateTime startOfLastMonth = new DateTime(todayDate.Year, todayDate.Month, 1)
                                                .AddMonths(-1);

                DateTime startOfThisMonth = new DateTime(todayDate.Year, todayDate.Month, 1);

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
                s.TECH_NAME      AS techName,
                s.SERIAL_NO      AS serialNo,
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
            WHERE s.TECH_CODE = @techCode
              AND v.ExpectedDate IS NOT NULL
              AND v.ExpectedDate >= @startOfLastMonth
              AND v.ExpectedDate < @startOfThisMonth
            ORDER BY v.ExpectedDate, v.VisitNo;
        ";

                var result = await connection.QueryAsync<ServiceVisitMonthlyInfo>(
                    query,
                    new
                    {
                        techCode,
                        startOfLastMonth,
                        startOfThisMonth
                    });

                return result.ToList();
            }
        }

        //Get the due service visits 
        public async Task<List<ServiceVisitMonthlyInfo>> GetDueServiceVisits(string techCode)
        {
            List<ServiceVisitMonthlyInfo> servicesdateduevisits = new List<ServiceVisitMonthlyInfo>();
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
                    s.SERIAL_NO      AS serialNo,
                    v.VisitNo        AS expectedVisitNo,
                    CONVERT(char(10), v.ExpectedDate, 120) AS expectedVisitDate
                FROM TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        (1, s.SV1, s.EXPT_SV1),
                        (2, s.SV2, s.EXPT_SV2),
                        (3, s.SV3, s.EXPT_SV3),
                        (4, s.SV4, s.EXPT_SV4),
                        (5, s.SV5, s.EXPT_SV5),
                        (6, s.SV6, s.EXPT_SV6)
                ) v (VisitNo, ActualVisit, ExpectedDate)
                WHERE s.TECH_CODE = @techcode
                  AND s.IS_ACTIVE = '0'
                  AND v.ActualVisit IS NULL                     -- not completed
                  AND v.ExpectedDate IS NOT NULL
                  AND v.ExpectedDate <= CAST(GETDATE() AS DATE) -- not future
                ORDER BY s.T_ID DESC, v.VisitNo;
                ";

                var result = connection.Query<ServiceVisitMonthlyInfo>(query, new { techcode = techCode }).ToList();
                servicesdateduevisits = result; 
            }

            return servicesdateduevisits;
        }        

        //Get the previous visits for a machine ref 
        public async Task<List<PreviousServiceVisitModel>> GetPreviousServiceVisits(string techCode, string machineRefNo)
        {
            List<PreviousServiceVisitModel> previousServiceVisits = new List<PreviousServiceVisitModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                SELECT TOP 1
	                T_ID AS LatestRowId,
                    EXPT_SV1 AS exptsv1,
                    EXPT_SV2 AS exptsv2,
                    EXPT_SV3 AS exptsv3,
                    EXPT_SV4 AS exptsv4,
                    EXPT_SV5 AS exptsv5,
                    EXPT_SV6 AS exptsv6,
                    SV1, SV2, SV3, SV4, SV5, SV6
                FROM TBL_SERVICE_SCEDULE_UPDATE
                WHERE TECH_CODE = @techcode
                  AND MACHINE_REF = @machinerefno
                  AND IS_ACTIVE = '0'
                ORDER BY T_ID DESC;";
                var result = connection.Query<PreviousServiceVisitModel>(query, new { 
                    techcode = techCode, 
                    machinerefno = machineRefNo 
                }).ToList();

                previousServiceVisits = result;
            }

            return previousServiceVisits;
        }

        //Available visits count 
        private async Task<int> AvailableLatestVisit(int jobID, string serialNo, int visitsPerYear, SqlConnection connection)
        {
            string query = @"
            SELECT TOP 1 SV1, SV2, SV3, SV4, SV5, SV6
            FROM TBL_SERVICE_SCEDULE_UPDATE
            WHERE SERIAL_NO = @SerialNo AND T_ID = @rowid            
            ";

            var result = await connection.QueryFirstOrDefaultAsync<ServiceVisit>(
                query, new { SerialNo = serialNo, rowid = jobID });

            if (result == null)
                return 1;

            DateTime?[] svArray =
            {
            result.SV1, result.SV2, result.SV3,
            result.SV4, result.SV5, result.SV6
        };

            int latestCompleted = 0;

            for (int i = 0; i < visitsPerYear; i++)
            {
                if (svArray[i].HasValue)
                {
                    latestCompleted = i + 1;
                }
            }

            int nextVisit = latestCompleted + 1;

            return nextVisit <= visitsPerYear ? nextVisit : 0;
        }

        //Check for latest visits 
        private async Task<int> CheckForLatestVisits(int jobID, string serialNo, int visitsPerYear, SqlConnection connection)
        {
            int latestVisitNo = 0;
            string query = @"
                SELECT SV1, SV2, SV3, SV4, SV5, SV6
                FROM TBL_SERVICE_SCEDULE_UPDATE 
                WHERE SERIAL_NO = @serialno AND T_ID = @rowid";
            var result = connection.QueryFirstOrDefaultAsync<ServiceVisit>(
            query, new { SerialNo = serialNo, rowid = jobID });

            if (result.Result == null)
                return 0;

            DateTime?[] svArray =
            {
                    result.Result.SV1, result.Result.SV2, result.Result.SV3,
                    result.Result.SV4, result.Result.SV5, result.Result.SV6
            };

            for (int i = 0; i < visitsPerYear; i++)
            {
                if (svArray[i].HasValue)
                {
                    latestVisitNo = i + 1; // SV1 → 1, SV2 → 2, etc.
                }
            }

            return latestVisitNo;
        }

        //Update Service Schedule Visit 

        public async Task<ScheduleResponse> UpdateServiceSchedule(
                                                int jobID,
                                                string techCode,
                                                int visitNo,
                                                string machineRefNo,
                                                string jobStatus,
                                                int? meterReadingValue,
                                                int? hologramNumber,
                                                string solution,
                                                string? solutionCategory)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    //Declaration of variables 
                    string serialNo = "";
                    string customerCode = "";
                    string customerName = "";
                    string customerAdd1 = "";
                    string customerAdd2 = "";
                    string customerAdd3 = "";
                    string machineId = "";
                    string machineModel = "";
                    string techName = "";
                    string techMobile = "";
                    int serviceVisitsCount = 0;
                    bool isActive = false;
                    string jobType = "";

                    //get tech information  
                    string getMobileQuery = @"
                    SELECT MOBILE_NO 
                    FROM MTBL_TECH_OFFICERS
                    WHERE TECH_CODE = @techcode";

                    techMobile = connection.QuerySingle<string>(getMobileQuery, new { 
                        techcode = techCode
                    });

                    string selectMachineQuery = @"
                            SELECT
                            SERIAL_NO, 
                            CUS_CODE, 
                            CUS_NAME, 
                            M_LOC1, 
                            M_LOC2, 
                            M_LOC3, 
                            MACHINE_CODE, 
                            MACHINE_DESC, 
                            T_OFFICER_CODE,
                            T_OFFICER_NAME,
                            VISITS_PER_YEAR
                            FROM TBL_MACHINE_TRANSACTION WHERE COM_ID = '001' AND MACHINE_REF_CODE = @qnumber";
                    var machineInfo = await connection.QuerySingleOrDefaultAsync<dynamic>(selectMachineQuery, new { qnumber = machineRefNo });
                    if (machineInfo != null)
                    {
                        serialNo = machineInfo.SERIAL_NO;
                        customerCode = machineInfo.CUS_CODE;
                        customerName = machineInfo.CUS_NAME;
                        customerAdd1 = machineInfo.CUS_ADD1;
                        customerAdd2 = machineInfo.CUS_ADD2;
                        customerAdd3 = machineInfo.CUS_ADD3;
                        machineId = machineInfo.MACHINE_CODE;
                        machineModel = machineInfo.MACHINE_DESC;
                        techCode = machineInfo.T_OFFICER_CODE;
                        techName = machineInfo.T_OFFICER_NAME;
                        serviceVisitsCount = Convert.ToInt32(machineInfo.VISITS_PER_YEAR);
                    }

                    int latestVisits = await CheckForLatestVisits(jobID, serialNo, serviceVisitsCount, connection);
                    int availableVisit = await AvailableLatestVisit(jobID, serialNo, serviceVisitsCount, connection);

                    if (jobStatus == "started")
                    {
                        //check for recall table 
                        string RecallIDQuery = @"
                        SELECT RECALL_ID FROM 
                        TBL_SERVICE_SCEDULE_UPDATE
                        WHERE T_ID = @rowid
                        ";

                        int? recallID = connection.QuerySingleOrDefault<int?>(RecallIDQuery, new
                        {
                            rowid = jobID
                        });

                        if (recallID.HasValue && recallID.Value > 0)
                        {
                            //Update the recall table
                            string updateRecallQuery = @"
                            UPDATE TBL_RECALL_VISIT 
                            SET SERVICE_STATUS = @status
                            WHERE RECALL_ID = @recallid";

                            connection.Execute(updateRecallQuery, new
                            {
                                status = jobStatus, 
                                recallid = recallID
                            });

                            jobType = "Service Recall";
                        }
                        else
                        {
                            jobType = "Normal Service";
                        }

                        string checkForActivityQuery = @"
                            SELECT CASE 
                                WHEN EXISTS (
                                    SELECT 1 
                                    FROM TBL_SCHEDULE_ACTIVITY 
                                    WHERE ROW_ID = @jobID 
                                      AND VISIT_NO = @visitNo
                                )
                                THEN CAST(1 AS BIT)
                                ELSE CAST(0 AS BIT)
                            END
                            ";

                        bool hasActivity = await connection.QuerySingleAsync<bool>(checkForActivityQuery, new { jobID, visitNo });

                        if (!hasActivity)
                        {
                            string insertActivityQuery = @"
                                INSERT INTO TBL_SCHEDULE_ACTIVITY    
                                (ROW_ID, VISIT_NO, STARTED_BY, STARTED_DATE, REASON, SOLUTION_CATEGORY, TYPE, IS_RECALL, RECALL_ID)
                                VALUES 
                                (@rowid, @visitno, @startedby, @starteddate, @solution, @solutioncategory, @type, @isrecall, @recallid)";
                            await connection.ExecuteAsync(insertActivityQuery, new
                            {
                                rowid = jobID,
                                visitno = visitNo,
                                startedby = techCode,
                                starteddate = GetSriLankanTime(),
                                solution = solution,
                                solutioncategory = solutionCategory,
                                type = "Job recall",
                                isrecall = true,
                                recallid = recallID
                            });
                        }

                        string updateQuery = $@"
                            UPDATE TBL_SERVICE_SCEDULE_UPDATE
                            SET 
                            SV{visitNo} = @visitdate, 
                            SV{visitNo}_STATUS = @jobstatus,
                            SV{visitNo}_MR = @meterreading
                            WHERE TECH_CODE = @techcode
                            AND MACHINE_REF = @machinerefno 
                            AND T_ID = @rowid";

                        var result = connection.Execute(updateQuery, new
                        {
                            visitdate = GetSriLankanTime(),
                            jobstatus = jobStatus,
                            meterreading = meterReadingValue,
                            techcode = techCode,
                            machinerefno = machineRefNo,
                            rowid = jobID
                        });
                    }
                    else if (jobStatus == "COMPLETED")
                    {
                        //check for recall table 
                        string RecallIDQuery = @"
                        SELECT RECALL_ID FROM 
                        TBL_SERVICE_SCEDULE_UPDATE
                        WHERE T_ID = @rowid
                        ";

                        int? recallID = connection.QuerySingleOrDefault<int?>(RecallIDQuery, new
                        {
                            rowid = jobID
                        });

                        if (recallID.HasValue && recallID.Value > 0)
                        {
                            //Update the recall table
                            string updateRecallQuery = @"
                            UPDATE TBL_RECALL_VISIT 
                            SET SERVICE_STATUS = @status
                            WHERE RECALL_ID = @recallid";

                            connection.Execute(updateRecallQuery, new
                            {
                                status = jobStatus,
                                recallid = recallID
                            });
                        }

                        string checkForActivityQuery = @"
                            SELECT CASE 
                                WHEN EXISTS (
                                    SELECT 1 
                                    FROM TBL_SCHEDULE_ACTIVITY 
                                    WHERE ROW_ID = @jobID 
                                      AND VISIT_NO = @visitNo
                                )
                                THEN CAST(1 AS BIT)
                                ELSE CAST(0 AS BIT)
                            END
                            ";

                        bool hasActivity = await connection.QuerySingleAsync<bool>(checkForActivityQuery, new { jobID, visitNo });

                        if (hasActivity)
                        {
                            string updateActivityQuery = @"
                            UPDATE TBL_SCHEDULE_ACTIVITY SET COMPLETED_BY = @techcode, COMPLETED_DATE = @completedate,
                            REASON = @note, SOLUTION_CATEGORY = @solution, TYPE = @jobrecall, IS_RECALL = '1'  
                            WHERE ROW_ID = @jobid";

                            connection.Execute(updateActivityQuery, new
                            {
                                techcode = techCode, 
                                completedate = GetSriLankanTime(), 
                                note = solution, 
                                solution = solutionCategory, 
                                jobrecall = jobType,
                                jobid = jobID
                            });
                        }

                        string updateQuery = $@"
                            UPDATE TBL_SERVICE_SCEDULE_UPDATE
                            SET 
                            SV{visitNo} = @visitdate, 
                            SV{visitNo}_STATUS = @jobstatus,
                            SV{visitNo}_MR = @meterreading
                            WHERE TECH_CODE = @techcode
                            AND MACHINE_REF = @machinerefno 
                            AND T_ID = @rowid";

                        var result = connection.Execute(updateQuery, new
                        {
                            visitdate = GetSriLankanTime(),
                            jobstatus = jobStatus,
                            meterreading = meterReadingValue,
                            techcode = techCode,
                            machinerefno = machineRefNo,
                            rowid = jobID
                        });
                    }
                }

                return new ScheduleResponse { 
                    errorMessage = "Successfully Updated", 
                    isUpdate = true, 
                    statusCode = StatusCodes.Status200OK.ToString()
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //public async Task<ScheduleResponse> UpdateServiceSchedule(
        //                                        int jobID,
        //                                        string techCode, 
        //                                        int visitNo, 
        //                                        string machineRefNo, 
        //                                        string jobStatus, 
        //                                        int? meterReadingValue, 
        //                                        int? hologramNumber, 
        //                                        string? solution)
        //{
        //    using (SqlConnection connection = new SqlConnection(_connectionString))
        //    {
        //        //Declaration of variables 
        //        string serialNo = "";
        //        string customerCode = "";
        //        string customerName = "";
        //        string customerAdd1 = "";
        //        string customerAdd2 = "";
        //        string customerAdd3 = "";
        //        string machineId = "";
        //        string machineModel = "";
        //        string techName = "";
        //        string techMobile = "";
        //        int serviceVisitsCount = 0;
        //        bool isActive = false;

        //        await connection.OpenAsync();

        //        string getTechMobileNo = @"SELECT MOBILE_NO FROM MTBL_TECH_OFFICERS WHERE TECH_CODE = @techcode";

        //        techMobile = await connection.QuerySingleOrDefaultAsync<string>(getTechMobileNo, new { techcode = techCode });

        //        string selectMachineQuery = @"
        //        SELECT
        //        SERIAL_NO, 
        //        CUS_CODE, 
        //        CUS_NAME, 
        //        M_LOC1, 
        //        M_LOC2, 
        //        M_LOC3, 
        //        MACHINE_CODE, 
        //        MACHINE_DESC, 
        //        T_OFFICER_CODE,
        //        T_OFFICER_NAME,
        //        VISITS_PER_YEAR
        //        FROM TBL_MACHINE_TRANSACTION WHERE COM_ID = '001' AND MACHINE_REF_CODE = @qnumber";
        //        var machineInfo = await connection.QuerySingleOrDefaultAsync<dynamic>(selectMachineQuery, new { qnumber = machineRefNo });
        //        if (machineInfo != null)
        //        {
        //            serialNo = machineInfo.SERIAL_NO;
        //            customerCode = machineInfo.CUS_CODE;
        //            customerName = machineInfo.CUS_NAME;
        //            customerAdd1 = machineInfo.CUS_ADD1;
        //            customerAdd2 = machineInfo.CUS_ADD2;
        //            customerAdd3 = machineInfo.CUS_ADD3;
        //            machineId = machineInfo.MACHINE_CODE;
        //            machineModel = machineInfo.MACHINE_DESC;
        //            techCode = machineInfo.T_OFFICER_CODE;
        //            techName = machineInfo.T_OFFICER_NAME;
        //            serviceVisitsCount = Convert.ToInt32(machineInfo.VISITS_PER_YEAR);
        //        }

        //        int latestVisits = await CheckForLatestVisits(jobID, serialNo, serviceVisitsCount, connection);
        //        int availableVisit = await AvailableLatestVisit(jobID, serialNo, serviceVisitsCount, connection);

        //        if (jobStatus == "COMPLETED")
        //        {
        //            //Updating the service schedule table 
        //            string updateServiceVisitQuery = $@"
        //            UPDATE TBL_SERVICE_SCEDULE_UPDATE
        //            SET                     
        //            SV{visitNo}_STATUS = 'COMPLETED',
        //            SV{visitNo}_SMS = @visitdate,
        //            SV{visitNo}_MR = @meterreading, 
        //            IS_ACTIVE = @isactive
        //            WHERE TECH_CODE = @techcode
        //            AND MACHINE_REF = @machinerefno 
        //            AND T_ID = @rowid
        //            ";                    

        //            //Inserting record to SS Visits SMS table 
        //            string insertSSVisitSMS = @"
        //            INSERT INTO TBL_SS_VISITS_SMS
        //            (COM_ID, 
        //            MOBILE_NO, 
        //            TECH_CODE, 
        //            SERIAL_NO, 
        //            MACHINE_REF_CODE,
        //            METER_READING, 
        //            VISIT_DATE,
        //            T_STATUS, 
        //            CONFIRM_BY, 
        //            CONFIRM_DATE)
        //            VALUES (
        //            @companyid,
        //            @mobileno, 
        //            @techcode, 
        //            @serialno, 
        //            @machinerefno, 
        //            @meterreading, 
        //            @visitdate, 
        //            @tstatus, 
        //            @confirmby, 
        //            @confirmdate)";

        //            var insertSSVisitSMSResult = await connection.ExecuteAsync(insertSSVisitSMS, new
        //            {
        //                companyid = "001",
        //                mobileno = techMobile, // Mobile number can be fetched and added here
        //                techcode = techCode,
        //                serialno = serialNo,
        //                machinerefno = machineRefNo,
        //                meterreading = meterReadingValue,
        //                visitdate = GetSriLankanTime().Date,
        //                tstatus = jobStatus,
        //                confirmby = techCode,
        //                confirmdate = GetSriLankanTime().Date
        //            });

        //            //Get the latest MeterReadingID for TBL_METER_READING table 
        //            string getLastMRID = @"SELECT ISNULL(MAX(MR_ID), 0) FROM TBL_METER_READING"; 
        //            int lastMRID = 0;
        //            var lastMRIDResult = await connection.QuerySingleOrDefaultAsync<int>(getLastMRID);
        //            int newmrId = lastMRIDResult + 1;

        //            string insertMeterReadingQuery = @"
        //            INSERT INTO TBL_METER_READING 
        //            (MR_ID, COM_ID, SERIAL_NO, MACHINE_REF_NO, CUS_ID, CUS_NAME, ADD1, ADD2, ADD3, MR_DATE, 
        //            REMARKS, MACHINE_ID, MACHINE_NAME, TECH_CODE, TECH_NAME, PURPOSE_OF_VISIT, 
        //            NO_OF_VISIT, MILAGE_START, MILAGE_END, EXPENCES, CATEGORY, 
        //            METER_TOTAL_COPY, BLACK_COPIES, COLOR_COPIES, METER_MASTER, MR_ADDED_DATE, CURRENT_SERVICE_DATE, SS_ID, SERVICES_DOC_RECIVED_DATE, 
        //            SMS_RECIVED_DATE, CR_BY)
        //            VALUES (
        //            @mrid, @companyid, @serialno, @machinerefno, @cusid, @cusname, @add1, @add2, @add3, @mrdate,
        //            @remarks, @machineid, @machinename, @techcode, @techname, @purposeofvisit,
        //            @noofvisit, @milagestart, @milageend, @expences, @category,
        //            @mertotcopy, @blackcopies, @colorcopies, @metermaster, @mradeddate, @currentservicedate, @ssid, @servicesdocreciveddate,
        //            @smsreciveddate, @crby
        //            )";

        //            string insertServiceVisitSolutionQuery = @"
        //            INSERT INTO TBL_SERVICE_VISIT_SOLUTION 
        //            (SERIAL_NO, MACHINE_REF_NO, TECH_CODE, TECH_NAME, JOB_ID, SOLUTION, JOB_STATUS, VISIT_NO) 
        //            VALUES 
        //            (@serialno, @machinerefno, @techcode, @techname, @jobid, @solution, @jobstatus, @visitno)";

        //            var insertServiceVisitSolutionResult = await connection.ExecuteAsync(insertServiceVisitSolutionQuery, new
        //            {
        //                serialno = serialNo,
        //                machinerefno = machineRefNo,
        //                techcode = techCode,
        //                techname = techName,
        //                jobid = jobID,
        //                solution = solution,
        //                jobstatus = jobStatus,
        //                visitno = visitNo
        //            });

        //            var insertMeterReadingResult = await connection.ExecuteAsync(insertMeterReadingQuery, new
        //            {
        //                mrid = newmrId,
        //                companyid = "001",
        //                serialno = serialNo,
        //                machinerefno = machineRefNo,
        //                cusid = customerCode,
        //                cusname = customerName,
        //                add1 = customerAdd1,
        //                add2 = customerAdd2,
        //                add3 = customerAdd3,
        //                mrdate = GetSriLankanTime().Date,
        //                remarks = solution,
        //                machineid = machineId,
        //                machinename = machineModel,
        //                techcode = techCode,
        //                techname = techName,
        //                purposeofvisit = "Service Visit",
        //                noofvisit = visitNo,
        //                milagestart = 0,
        //                milageend = meterReadingValue,
        //                expences = 0,
        //                category = "SERVICE",
        //                mertotcopy = meterReadingValue,
        //                blackcopies = 0,
        //                colorcopies = 0,
        //                metermaster = meterReadingValue,
        //                mradeddate = GetSriLankanTime().Date,
        //                currentservicedate = GetSriLankanTime().Date,
        //                ssid = jobID,
        //                servicesdocreciveddate = GetSriLankanTime().Date,
        //                smsreciveddate = GetSriLankanTime().Date,
        //                crby = techCode
        //            });

        //            if (visitNo == 4)
        //            {
        //                string query = $@"
        //                SELECT
        //                    CASE
        //                        WHEN SV1 IS NULL
        //                         AND SV2 IS NULL
        //                         AND SV3 IS NULL
        //                         AND SV4 IS NULL
        //                         AND SV5 IS NULL
        //                         AND SV6 IS NULL
        //                        THEN 1
        //                        ELSE 0
        //                    END AS IsAllNull
        //                FROM TBL_SERVICE_SCEDULE_UPDATE
        //                WHERE T_ID = @jobid;
        //                ";
        //                bool isAllCompleted = connection.QuerySingle<bool>(query, new { jobid = jobID});
        //                if (isAllCompleted == false)
        //                {
        //                    isActive = true;
        //                }
        //                else
        //                {
        //                    isActive = false;
        //                }
        //            }

        //            var updateServiceVisitResult = await connection.ExecuteAsync(updateServiceVisitQuery, new
        //            {
        //                visitdate = GetSriLankanTime().Date,                        
        //                meterreading = meterReadingValue,
        //                techcode = techCode,
        //                machinerefno = machineRefNo,
        //                rowid = jobID, 
        //                isactive = isActive
        //            });

        //            if ((updateServiceVisitResult > 0) && (insertSSVisitSMSResult > 0))
        //            {
        //                return new ScheduleResponse
        //                {
        //                    statusCode = StatusCodes.Status200OK.ToString(),
        //                    errorMessage = $"Your visit has been successfully updated. Visit No {visitNo}.",
        //                    isUpdate = true
        //                };
        //            }
        //        }

        //        string updateQuery = $@"
        //            UPDATE TBL_SERVICE_SCEDULE_UPDATE
        //            SET 
        //            SV{visitNo} = @visitdate, 
        //            SV{visitNo}_STATUS = @jobstatus,
        //            SV{visitNo}_MR = @meterreading
        //            WHERE TECH_CODE = @techcode
        //            AND MACHINE_REF = @machinerefno 
        //            AND T_ID = @rowid
        //            ";

        //        var result = connection.Execute(updateQuery, new
        //        {
        //            visitdate = GetSriLankanTime().Date,
        //            jobstatus = jobStatus,
        //            meterreading = meterReadingValue,
        //            techcode = techCode,
        //            machinerefno = machineRefNo,
        //            rowid = jobID
        //        });

        //        if (result > 0)
        //        {
        //            return new ScheduleResponse
        //            {
        //                statusCode = StatusCodes.Status200OK.ToString(),
        //                errorMessage = $"Your visit has been successfully updated. Visit No {visitNo}.",
        //                isUpdate = true
        //            };
        //        }
        //        else
        //        {
        //            return new ScheduleResponse
        //            {
        //                statusCode = StatusCodes.Status400BadRequest.ToString(),
        //                errorMessage = "No record was updated. Please check the visit number.",
        //                isUpdate = false
        //            };
        //        }
        //    }
        //}        

        private string CheckForExistingVisits(string serialNo, string visitColumn, SqlConnection connection)
        {
            string query = $@"
            SELECT TOP 1 T_ID 
            FROM TBL_SERVICE_SCEDULE_UPDATE 
            WHERE COM_ID = '001'
            AND SERIAL_NO = @serialno
            AND IS_ACTIVE = '1'
            AND ({visitColumn}_SMS IS NULL OR {visitColumn} IS NULL)
            ";

            var result = connection.QueryFirstOrDefault<string>(query, new { serialno = serialNo });
            return result;
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
                    c.TEL_NO      AS customerTelephone,
                    s.M_LOC1      AS machineLocation01, 
                    s.M_LOC2      AS machineLocation02, 
                    s.M_LOC3      AS machineLocation03, 
                    s.MACHINE_REF AS machineRefNo,
                    s.SERIAL_NO   AS serialNo,
                    v.VisitNo     AS expectedVisitNo,
                    CONVERT(char(10), v.ExpectedDate, 120) AS expectedVisitDate,
                    v.VisitStatus AS VisitStatus
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        ('EXPT_SV1', s.EXPT_SV1, s.SV1, s.SV1_STATUS),
                        ('EXPT_SV2', s.EXPT_SV2, s.SV2, s.SV2_STATUS),
                        ('EXPT_SV3', s.EXPT_SV3, s.SV3, s.SV3_STATUS),
                        ('EXPT_SV4', s.EXPT_SV4, s.SV4, s.SV4_STATUS),
                        ('EXPT_SV5', s.EXPT_SV5, s.SV5, s.SV5_STATUS),
                        ('EXPT_SV6', s.EXPT_SV6, s.SV6, s.SV6_STATUS)
                ) v (VisitNo, ExpectedDate, ActualVisit, VisitStatus)
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

public class ServiceVisit
{
    public DateTime? SV1 { get; set; }
    public DateTime? SV2 { get; set; }
    public DateTime? SV3 { get; set; }
    public DateTime? SV4 { get; set; }
    public DateTime? SV5 { get; set; }
    public DateTime? SV6 { get; set; }
}

public class ScheduleResponse
{
    public string statusCode { get; set; }
    public string errorMessage { get; set; }
    public bool isUpdate { get; set; }
}
