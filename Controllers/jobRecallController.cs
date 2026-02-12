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
        


        //POST: api/jobRecall/recallJob
        [Authorize]
        [HttpPost("recallJob")]
        public IActionResult RecallJob([FromBody] BreakdownJobsRecallModel model) 
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();                
                string insertJobRecallQuery = @"INSERT INTO TBL_RECALL_JOBS (RECALL_REASON, RECALL_DATE, JOB_ID, IS_RECALL)
                                            VALUES (@recallReason, @recallDate, @jobId, '1')";

                DateTime recallDate = GetSriLankanTime();
                var insertedResult = connection.Execute(insertJobRecallQuery, new { recallReason = model.reason, recallDate = recallDate, jobId = model.jobID });

                if (insertedResult > 0)
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
