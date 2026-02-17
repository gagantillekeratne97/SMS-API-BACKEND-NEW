using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServvistaWebAppAPI.Models;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class jobRecallController : ControllerBase
    {
        private readonly string _connectionString;

        public jobRecallController(IConfiguration config)
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

        //GET: api/jobRecall/getallRecallJobs
        [Authorize]
        [HttpGet("getallRecallJobs")]
        public IActionResult GetAllRecallJobs(string techCode) 
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"SELECT 
                        JOB_ID AS DJ_ID,
                        *
                    FROM TBL_RECALL_JOBS
                    WHERE TECH_CODE = @techcode
                    ";
                    var result = connection.Query<BreakdownModel>(query, new { techcode = techCode });
                    return Ok(result);
                }               
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
                
        //POST: api/jobRecall/recallJob
        [Authorize]
        [HttpPost("recallJob")]
        public async Task<IActionResult> RecallJob([FromBody] BreakdownJobsRecallModel model)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Get daily jobs information 
                    string getJobInfoQuery = @"
            SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, 
                   CUS_CONTACT, CUS_TYPE, CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, 
                   TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS, JOB_STATUS
            FROM TBL_DAILY_JOBS
            WHERE DJ_ID = @jobid";

                    var jobInfo = await connection.QuerySingleOrDefaultAsync<dynamic>(getJobInfoQuery, new { jobid = model.jobID });

                    if (jobInfo == null)
                    {
                        return BadRequest("Job not found");
                    }

                    // 2. Insert into TBL_RECALL_JOBS and get RECALL_ID
                    string insertJobRecallQuery = @"
            INSERT INTO TBL_RECALL_JOBS 
            (RECALL_REASON, RECALL_DATE, JOB_ID, IS_RECALL, SERIAL_NO, MACHINE_REF_NO, 
             CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, CUS_TEL_NO, TEAM_ID, 
             TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, 
             CUS_STATUS, NOTE, JOB_STATUS, IS_TECH_NOTIFIED, TYPE)
            OUTPUT INSERTED.RECALL_ID
            VALUES 
            (@recallreason, @recalldate, @jobid, @isrecall, @serialno, @machinerefno, 
             @cusname, @cusadd1, @cusadd2, @cusadd3, @cuscontact, @custelno, @teamid, 
             @teamname, @djdate, @techcode, @techmobile, @machinemodelid, @machinemodelname, 
             @cusstatus, @note, @jobstatus, @istechnotified, @type)";

                    DateTime recallDate = GetSriLankanTime();

                    // FIX: Use QuerySingle instead of Execute to get the RECALL_ID
                    int recallID = await connection.QuerySingleAsync<int>(insertJobRecallQuery, new
                    {
                        recallreason = model.reason,
                        recalldate = recallDate,
                        jobid = model.jobID,
                        isrecall = true,
                        serialno = jobInfo.SERIAL_NO,
                        machinerefno = jobInfo.MACHINE_REF_NO,
                        cusname = jobInfo.CUS_NAME,
                        cusadd1 = jobInfo.CUS_ADD1,
                        cusadd2 = jobInfo.CUS_ADD2,
                        cusadd3 = jobInfo.CUS_ADD3,
                        cuscontact = jobInfo.CUS_CONTACT,
                        custelno = jobInfo.CUS_TEL_NO,
                        teamid = jobInfo.TEAM_ID,
                        teamname = jobInfo.TEAM_NAME,
                        djdate = jobInfo.DJ_DATE,
                        techcode = jobInfo.TECH_CODE,
                        techmobile = jobInfo.TECH_MOBILE,
                        machinemodelid = jobInfo.MACHINE_MODEL_ID,
                        machinemodelname = jobInfo.MACHINE_MODEL_NAME,
                        cusstatus = jobInfo.CUS_STATUS,
                        note = model.note,
                        jobstatus = jobInfo.JOB_STATUS,
                        istechnotified = true,
                        type = "Job recall"
                    });

                    // 3. Insert into TBL_SCHEDULE_ACTIVITY
                    string insertActivityQuery = @"
            INSERT INTO TBL_SCHEDULE_ACTIVITY 
            (ROW_ID, STARTED_BY, STARTED_DATE, REASON, SOLUTION_CATEGORY, TYPE, IS_RECALL, RECALL_ID) 
            VALUES 
            (@jobid, @startedby, @starteddate, @reason, @solution, @type, 1, @recallid)";

                    await connection.ExecuteAsync(insertActivityQuery, new
                    {
                        jobid = model.jobID,
                        startedby = model.techCode,
                        starteddate = recallDate,
                        reason = model.reason,
                        solution = jobInfo.CUS_TYPE,
                        type = "Job recall",
                        recallid = recallID
                    });

                    // 4. Update TBL_DAILY_JOBS with RECALL_ID
                    string updateRecallJobQuery = @"
            UPDATE TBL_DAILY_JOBS 
            SET JOB_STATUS = 'started', 
                STARTED_BY = @techcode, 
                RECALL_ID = @recallid, 
                STARTED_DATE = @starteddate
            WHERE DJ_ID = @jobid";

                    await connection.ExecuteAsync(updateRecallJobQuery, new
                    {
                        techcode = model.techCode,
                        starteddate = recallDate,
                        jobid = model.jobID,
                        recallid = recallID
                    });

                    return Ok(new { message = "Recall Job Updated Successfully", recallId = recallID });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);                
            }
        }

        //GET: api/jobRecall/getAllLastYearsJobs?techCode={techCode}
        [Authorize]
        [HttpGet("getAllLastYearsJobs")]
        public IActionResult GetAllLastYearJobs(string techCode)
        {
            try
            {
                string jobType = "Due";
                DateTime now = DateTime.UtcNow.AddDays(-1);
                DateTime oneYearBack = now.AddYears(-1);

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                    SELECT * FROM TBL_DAILY_JOBS 
                    WHERE TECH_CODE = @techcode 
                    AND DJ_DATE >= @oneYearBack 
                    AND DJ_DATE <= @now
                    AND JOB_STATUS IN ('TECH ALLOCATED', 'started')";

                    var result = connection.Query<BreakdownModel>(query,
                        new
                        {
                            techcode = techCode,
                            oneYearBack = oneYearBack,
                            now = now
                        })
                        .Select(x => { x.TYPE = jobType; return x; })
                        .ToList();

                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
