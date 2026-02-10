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

        //POST: api/jobRecall/recallJob
        [Authorize]
        [HttpPost("recallJob")]
        public IActionResult RecallJob() 
        {
            return Ok("This endpoint is under construction. Please check back later.");
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
                    WHERE TECH_CODE = @techcode AND DJ_DATE >= @firstdayoflastyear AND DJ_DATE <= @lastoflastyear AND JOB_STATUS = 'TECH ALLOCATED'";
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
