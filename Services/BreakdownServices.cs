using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;
using System.Data.SqlClient;
using System.Reflection.Metadata;

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

        public async Task<List<BreakdownCategoriesModel>> GetSolutionCategoriesAsync()
        {
            List<BreakdownCategoriesModel> breakdownCategories = new List<BreakdownCategoriesModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"SELECT ID, SOLUTION_CATEGORY, SOLUTION_SHORT_CATEGORY FROM TBL_BREAKDOWN_CATEGORY";
                var result = await connection.QueryAsync<BreakdownCategoriesModel>(query);
                breakdownCategories = result.ToList();
            }

            return breakdownCategories;
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
                JOB_STATUS AS JOB_STATUS, NOTE AS NOTE
                FROM TBL_DAILY_JOBS 
                WHERE TECH_CODE = @techcode AND DJ_DATE >= @assigneddate AND DJ_DATE <= @dayafterassigneddate AND JOB_STATUS <> 'CANCELLED'";

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

        public async Task<List<BreakdownModel>> GetDueJobsLists(string techCode)
        {
            List<BreakdownModel> breakdownModels = new List<BreakdownModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, 
                    CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS, JOB_STATUS
                    FROM TBL_DAILY_JOBS
                    WHERE TECH_CODE = @techcode
                    AND JOB_STATUS = 'TECH ALLOCATED'";
                var result = connection.Query<BreakdownModel>(query, new { techcode = techCode });
                breakdownModels = result.ToList();
            }

            return breakdownModels; 
        }

        public async Task<List<BreakdownModel>> GetPendingLists(string techCode) 
        {
            List<BreakdownModel> breakdownModel = new List<BreakdownModel>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, 
                    CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS, JOB_STATUS
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); 
                DateTime todayDate = GetSriLankanTime().Date;
                DateTime startOfLastMonth = new DateTime(todayDate.Year, todayDate.Month, 1).AddMonths(-1);
                DateTime startOfThisMonth = new DateTime(todayDate.Year, todayDate.Month, 1);

                string query = @"
                SELECT * 
                FROM TBL_DAILY_JOBS 
                WHERE TECH_CODE = @techcode                 
                AND JOB_STATUS <> 'CANCELLED'
                "; 
                var breakdowns = await connection.QueryAsync<BreakdownModel>(query, new
                {
                    techcode = techCode,
                    startoflastmonth = startOfLastMonth.Date,
                    startofthismonth = startOfThisMonth.Date
                });
                
                return breakdowns.ToList();
            }
        }

        //Update Breakdown Jobs        
        public async Task UpdateJobStatus(UpdateJobModel model)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    int jobId = model.jobId;
                    string machineRefNo = model.machineRefNo;
                    string techCode = model.techCode;
                    string note = model.Note;
                    string jobStatus = model.jobStatus;
                    bool isJobExists = false;
                    string solutionCategory = model.solutionCategory;
                    string type = "Job";

                    //check for job exists 
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

                    bool IsJobExists = await connection.QuerySingleAsync<bool>(checkJobIDExists, new { techcode = techCode, jobid = jobId });

                    if (IsJobExists == true)
                    {
                        if (jobStatus == "started")
                        {
                            //Updating main Daily jobs table 
                            string updateJobQuery = @"
                            UPDATE TBL_DAILY_JOBS 
                            SET 
                            JOB_STATUS = @jobstatus, 
                            STARTED_BY = @techcode, 
                            STARTED_DATE = @starteddate
                            WHERE DJ_ID = @jobid AND TECH_CODE = @techcode
                            ";

                            DateTime startedDate = GetSriLankanTime();
                            await connection.ExecuteAsync(updateJobQuery, new
                            {
                                jobid = jobId,
                                jobstatus = jobStatus,
                                techcode = techCode,
                                starteddate = startedDate
                            });

                            int? recallID = null;

                            string checkForRecallQuery = @"
                            SELECT RECALL_ID
                            FROM TBL_DAILY_JOBS
                            WHERE DJ_ID = @jobid";

                            int? recallId = connection.QuerySingleOrDefault<int?>(
                                checkForRecallQuery,
                                new { jobid = jobId }
                            );

                            if (recallId.HasValue && recallId.Value > 0)
                            {
                                int actualRecallId = recallId.Value;
                                string updateRecallQuery = @"
                                UPDATE TBL_RECALL_JOBS SET JOB_STATUS = @jobstatus
                                WHERE JOB_ID = @jobid AND RECALL_ID = @recallid";

                                connection.Execute(updateRecallQuery, new { 
                                    jobstatus = jobStatus, 
                                    jobid = jobId, 
                                    recallid = actualRecallId
                                });
                            }
                            string updateActivityQuery = @"
                            UPDATE TBL_SCHEDULE_ACTIVITY 
                            SET STARTED_BY = @techcode, STARTED_DATE = @starteddate
                            WHERE ROW_ID = @jobid";

                            connection.Execute(updateActivityQuery, new { 
                                techcode = techCode, 
                                startedDate = GetSriLankanTime(), 
                                jobid = jobId
                            });
                        }
                        else if (jobStatus == "COMPLETE")
                        {
                            DateTime completeDate = GetSriLankanTime();

                            //updating daily jobs table 
                            string updateJobQuery = @"
                            UPDATE TBL_DAILY_JOBS 
                            SET
                            JOB_STATUS = @jobstatus, 
                            SOLUTION_CATEGORY = @solutioncategory, 
                            COMPLETE_SOLUTION = @note, 
                            COMPLETE_BY = @techcode, 
                            COMPLETED_DATE = @completeddate
                            WHERE DJ_ID = @jobid AND TECH_CODE = @techcode";

                            await connection.ExecuteAsync(updateJobQuery, new
                            {
                                jobstatus = jobStatus,
                                solutioncategory = solutionCategory,
                                note = note,
                                techcode = techCode,
                                completeddate = completeDate,
                                jobid = jobId
                            });

                            // 2. Check if RECALL_ID exists
                            string checkForRecallQuery = @"
                            SELECT RECALL_ID
                            FROM TBL_DAILY_JOBS
                            WHERE DJ_ID = @jobid";

                            int? recallId = await connection.QuerySingleOrDefaultAsync<int?>(checkForRecallQuery, new { jobid = jobId });

                            // 3. If recall exists - Update TBL_RECALL_JOBS
                            if (recallId.HasValue && recallId.Value > 0)
                            {
                                string updateRecallTableQuery = @"
                                UPDATE TBL_RECALL_JOBS 
                                SET JOB_STATUS = @status, 
                                NOTE = @note 
                                WHERE RECALL_ID = @recallid";

                                await connection.ExecuteAsync(updateRecallTableQuery, new
                                {
                                    status = jobStatus,
                                    note = note,
                                    recallid = recallId.Value
                                });
                            }

                            // 4. Update TBL_SCHEDULE_ACTIVITY
                            string updateActivityQuery = @"
                            UPDATE TBL_SCHEDULE_ACTIVITY 
                            SET COMPLETED_BY = @completedby, 
                                COMPLETED_DATE = @completeddate, 
                                REASON = @reason, 
                                SOLUTION_CATEGORY = @solution
                            WHERE ROW_ID = @jobid";

                            await connection.ExecuteAsync(updateActivityQuery, new
                            {
                                jobid = jobId,
                                completedby = techCode,
                                completeddate = completeDate,
                                reason = note,
                                solution = solutionCategory
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //public async Task UpdateJobStatus(UpdateJobModel model)
        //{
        //    using (SqlConnection connection = new SqlConnection(_connectionString))
        //    {
        //        int jobId = model.jobId; 
        //        string machineRefNo = model.machineRefNo;
        //        string techCode = model.techCode;
        //        string note = model.Note; 
        //        string jobStatus = ""; 

        //        connection.Open();         

        //        jobStatus = model.jobStatus;

        //        string checkJobIDExists = @"
        //                                SELECT 
        //                                    CASE 
        //                                        WHEN EXISTS (
        //                                        SELECT 1 FROM TBL_DAILY_JOBS 
        //                                        WHERE DJ_ID = @jobid AND TECH_CODE = @techcode
        //                                        )
        //                                        THEN CAST(1 AS BIT) 
        //                                        ELSE CAST(0 AS BIT)
        //                                    END AS JobExists";
        //        bool IsJobExists = await connection.QuerySingleAsync<bool>(checkJobIDExists, new { techcode = techCode, jobid = jobId});

        //        if (IsJobExists) 
        //        {
        //            if (jobStatus == "COMPLETE")
        //            {
        //                //This is updated
        //                string updateJobQuery = @"UPDATE TBL_DAILY_JOBS SET JOB_STATUS = @jobstatus, SOLUTION_CATEGORY = @custype, COMPLETE_BY = @techcode, COMPLETED_DATE = @completedate, COMPLETE_SOLUTION = @note
        //                                      WHERE DJ_ID = @jobid AND TECH_CODE = @techcode";
        //                DateTime completeDate = GetSriLankanTime();
        //                await connection.ExecuteAsync(updateJobQuery, new { jobid = jobId, jobstatus = jobStatus, custype = model.solutionCategory, techcode = techCode, completedate = completeDate, note = model.Note});
        //            } else if (jobStatus == "CANCELLED") 
        //            {
        //                string updateJobQuery = @"UPDATE TBL_DAILY_JOBS SET JOB_STATUS = @jobstatus, CANCELLED_BY = @techcode, CANCELLED_DATE = @cancelledate
        //                                      WHERE DJ_ID = @jobid AND TECH_CODE = @techcode";
        //                DateTime completeDate = GetSriLankanTime();
        //                await connection.ExecuteAsync(updateJobQuery, new { jobid = jobId, jobstatus = jobStatus, techcode = techCode, cancelledate = completeDate });
        //            }
        //            else
        //            {
        //                string updateJobQuery = @"UPDATE TBL_DAILY_JOBS SET JOB_STATUS = @jobstatus, CR_BY = @techcode, CR_DATE = @completedate, 
        //                                        STARTED_BY = @techcode, STARTED_DATE = @completedate
        //                                        WHERE DJ_ID = @jobid AND TECH_CODE = @techcode";
        //                DateTime completeDate = GetSriLankanTime();
        //                await connection.ExecuteAsync(updateJobQuery, new { jobid = jobId, jobstatus = jobStatus, techcode = techCode, completedate = completeDate });
        //            }
        //        }
        //    }
        //}

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
