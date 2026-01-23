using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Services
{
    public class BreakdownServices : IBreakdownServices
    {
        private readonly string _connectionString; 
        public BreakdownServices(IConfiguration config)
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

        public async Task<List<BreakdownModel>> GetTodayBreakdownList(string techCode)
        {
            List<BreakdownModel> breakdownModelsLists = new List<BreakdownModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, 
                CUS_SMS_NO AS CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS,
                JOB_STATUS AS jobStatus, NOTE AS note
                FROM TBL_DAILY_JOBS 
                WHERE TECH_CODE = @techcode AND DJ_DATE >= @assigneddate AND DJ_DATE <= @dayafterassigneddate";

                DateTime assignedDate = GetSriLankanTime().Date;
                DateTime dayafterassignedDate = GetSriLankanTime().AddDays(1).Date;
                breakdownModelsLists = connection.Query<BreakdownModel>(query, new
                {
                    techcode = techCode,
                    assigneddate = assignedDate,
                    dayafterassigneddate = dayafterassignedDate
                }).ToList();
            }

            return breakdownModelsLists;
        }

        public async Task<List<BreakdownModel>> GetPendingLists(string techCode) 
        {
            List<BreakdownModel> breakdownModel = new List<BreakdownModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, 
                    CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS
                    FROM TBL_DAILY_JOBS
                    WHERE TECH_CODE = @techcode AND DJ_DATE >= @assigneddate AND DJ_DATE <= @dayafterassigndate 
                    AND JOB_STATUS = 'TECH ALLOCATED'";                

                DateTime assignedDate = GetSriLankanTime().Date;
                DateTime dayafterassigndate = GetSriLankanTime().AddDays(1).Date;

                breakdownModel = connection.Query<BreakdownModel>(query, new
                {
                    techcode = techCode,
                    assigneddate = assignedDate,
                    dayafterassigndate = dayafterassigndate
                }).ToList();
            }

            return breakdownModel; 
        }

        //Get all total breakdown jobs
        public async Task<List<BreakdownModel>> GetTotalBreakdowns(string techCode)
        {
            List<BreakdownModel> breakdownModels = new List<BreakdownModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open(); 
                string query = @"SELECT * FROM TBL_DAILY_JOBS WHERE TECH_CODE = @techcode";
                var result = connection.Query<BreakdownModel>(query, new { techcode = techCode }).ToList();
                breakdownModels = result;
            }

            return breakdownModels; 
        }

        //Update Breakdown Jobs
        public async Task UpdateJobStatus(UpdateJobModel model)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                int jobId = model.jobId; 
                string machineRefNo = model.machineRefNo;
                string techCode = model.techCode;
                string note = model.Note; 
                string jobStatus = ""; 

                connection.Open();
                switch (model.jobStatus)
                {
                    case "Pending":
                        jobStatus = "TECH ALLOCATED";
                        break;
                    case "Started":
                        jobStatus = "TECH ALLOCATED"; 
                        break;
                    case "Completed":
                        jobStatus = "COMPLETE";
                        break;
                    case "Cancelled":
                        jobStatus = "CANCELLED";
                        break;
                    default:
                        jobStatus = model.jobStatus;
                        break;
                }

                string checkJobIDExists = @"
                                        SELECT 
                                            CASE 
                                                WHEN EXISTS (
                                                SELECT 1 FROM TBL_DAILY_JOBS 
                                                WHERE DJ_ID = @jobid AND TECH_CODE = @techcode
                                                )
                                                THEN CAST(1 AS BIT) 
                                                ELSE CAST(0 AS BIT)
                                            END AS JobExists";
                bool IsJobExists = await connection.QuerySingleAsync<bool>(checkJobIDExists, new { techcode = techCode, jobid = jobId});

                if (IsJobExists) {
                    if (jobStatus == "COMPLETE")
                    {
                        string updateJobQuery = @"UPDATE TBL_DAILY_JOBS SET JOB_STATUS = @jobstatus, COMPLETE_BY = @techcode, COMPLETED_DATE = @completedate
                                              WHERE DJ_ID = @jobid AND TECH_CODE = @techcode";
                        DateTime completeDate = GetSriLankanTime();
                        await connection.ExecuteAsync(updateJobQuery, new { jobid = jobId, jobstatus = jobStatus, techcode = techCode, completedate = completeDate });
                    } else if (jobStatus == "CANCELLED") 
                    {
                        string updateJobQuery = @"UPDATE TBL_DAILY_JOBS SET JOB_STATUS = @jobstatus, CANCELLED_BY = @techcode, CANCELLED_DATE = @cancelledate
                                              WHERE DJ_ID = @jobid AND TECH_CODE = @techcode";
                        DateTime completeDate = GetSriLankanTime();
                        await connection.ExecuteAsync(updateJobQuery, new { jobid = jobId, jobstatus = jobStatus, techcode = techCode, cancelledate = completeDate });
                    }
                }
            }
        }

        public async Task<List<BreakdownModel>> GetCompleteLists(string techCode)
        {
            string query = @"
                    SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, 
                    CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS
                    FROM TBL_DAILY_JOBS
                    WHERE TECH_CODE = @techcode AND DJ_DATE >= @assigneddate AND DJ_DATE <= @dayafterassigndate 
                    AND JOB_STATUS = 'COMPLETE'";
            List<BreakdownModel> breakdownModels = new List<BreakdownModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                DateTime assignedDate = GetSriLankanTime().Date;
                DateTime dayafterassigndate = GetSriLankanTime().AddDays(1).Date;

                breakdownModels = connection.Query<BreakdownModel>(query, new
                {
                    techcode = techCode,
                    assigneddate = assignedDate,
                    dayafterassigndate = dayafterassigndate
                }).ToList();
            }

            return breakdownModels; 
        }
    }
}
