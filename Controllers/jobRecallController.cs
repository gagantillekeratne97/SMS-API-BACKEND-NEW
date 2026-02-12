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
                    string query = @"SELECT * FROM TBL_RECALL_JOBS WHERE TECH_CODE = @techcode";
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
        public IActionResult RecallJob([FromBody] BreakdownJobsRecallModel model) 
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                //get daily jobs information 
                string getJobInfoQuery = @"
                    SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, 
                    CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS, JOB_STATUS
                    FROM TBL_DAILY_JOBS
                    WHERE DJ_ID = @jobid
                    ";

                var jobInforResult = connection.Query(getJobInfoQuery, new { jobid = model.jobID }).SingleOrDefault();                

                string serialNo = jobInforResult.SERIAL_NO; 
                string machineRefNo = jobInforResult.MACHINE_REF_NO; 
                string cusName = jobInforResult.CUS_NAME; 
                string cusAdd1 = jobInforResult.CUS_ADD1;
                string cusAdd2 = jobInforResult.CUS_ADD2; 
                string cusAdd3 = jobInforResult.CUS_ADD3; 
                string contact = jobInforResult.CUS_CONTACT;
                string cusTel = jobInforResult.CUS_TEL_NO; 
                string teamId = jobInforResult.TEAM_ID;
                string teanName = jobInforResult.TEAM_NAME;
                DateTime djDate = jobInforResult.DJ_DATE; 
                string techCode = jobInforResult.TECH_CODE; 
                string techMobile = jobInforResult.TECH_MOBILE;
                string machineModelID = jobInforResult.MACHINE_MODEL_ID;
                string machineModelName = jobInforResult.MACHINE_MODEL_NAME; 
                string cusStatus = jobInforResult.CUS_STATUS;
                string jobStatus = jobInforResult.JOB_STATUS;

                string insertJobRecallQuery = @"
                INSERT INTO TBL_RECALL_JOBS 
                (RECALL_REASON, RECALL_DATE, JOB_ID, IS_RECALL, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, 
                CUS_CONTACT, 
                CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, MACHINE_MODEL_NAME, CUS_STATUS, 
                NOTE, JOB_STATUS, 
                IS_TECH_NOTIFIED, TYPE)
                VALUES (
                @recallreason, @recalldate, @jobid, @isrecall, @serialno, @machinerefno, @cusname, @cusadd1, @cusadd2, @cusadd3,
                @cuscontact, @custelno, @teamid, @teamname, @djdate, @techcode, @techmobile, 
                @machinemodelid, @machinemodelname, @cusstatus, @note, @jobstatus, 
                @istechnotified, @type)";

                DateTime recallDate = GetSriLankanTime();

                var insertResult = connection.Execute(insertJobRecallQuery, new { 
                    recallreason = model.reason, 
                    recalldate = GetSriLankanTime(), 
                    jobid = model.jobID, 
                    isrecall = true, 
                    serialno = serialNo, 
                    machinerefno = machineRefNo, 
                    cusname = cusName, 
                    cusadd1 = cusAdd1, 
                    cusadd2 = cusAdd2,
                    cusadd3 = cusAdd3,
                    cuscontact = contact, 
                    custelno = cusTel, 
                    teamid = teamId, 
                    teamname = teanName, 
                    djdate = djDate, 
                    techcode = techCode, 
                    techmobile = techMobile, 
                    machinemodelid = machineModelID, 
                    machinemodelname = machineModelName, 
                    cusstatus = cusStatus, 
                    note = model.note,
                    jobstatus = jobStatus,
                    istechnotified = true, 
                    type = "Job recall"
                });                                

                if (insertResult > 0)
                {
                    string updateRecallJobQuery = @"UPDATE TBL_DAILY_JOBS SET JOB_STATUS = 'started', STARTED_BY = @techCode, 
                                             STARTED_DATE = @startedDate, DJ_DATE = @startedDate
                                             WHERE DJ_ID = @jobID";
                    DateTime startedDate = GetSriLankanTime();
                    connection.Execute(updateRecallJobQuery, new { techCode = model.techCode, startedDate = startedDate, jobID = model.jobID });
                }
            }
            return Ok("Recall Job Updated Successfully");
        }

        //GET: api/jobRecall/getAllLastYearsJobs?techCode={techCode}
        [Authorize]
        [HttpGet("getAllLastYearsJobs")]
        public IActionResult GetAllLastYearJobs(string techCode)
        {
            try
            {
                string jobType = "Due";
                DateTime now = DateTime.UtcNow;

                // Last year
                int lastYear = now.Year - 1;

                DateTime firstDayOfLastYear = new DateTime(lastYear, 1, 1);
                DateTime lastDayOfLastYear = new DateTime(lastYear, 12, 31, 23, 59, 59);

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                    SELECT * FROM TBL_DAILY_JOBS 
                    WHERE TECH_CODE = @techcode AND DJ_DATE >= @firstdayoflastyear AND DJ_DATE <= @lastoflastyear AND JOB_STATUS IN ('TECH ALLOCATED', 'started')";
                    var result = connection.Query<BreakdownModel>(query, new { techcode = techCode, firstdayoflastyear = firstDayOfLastYear, lastoflastyear = lastDayOfLastYear })
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
