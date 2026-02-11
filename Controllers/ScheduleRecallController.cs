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
    public class ScheduleRecallController : ControllerBase
    {
        private readonly string _connectionString;  
        public ScheduleRecallController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [Authorize]
        [HttpPost("recallPreviousSchedule")]
        public IActionResult RecallPreviousSchedule([FromBody] RecallPreviousScheduleModel model)
        {
            //changes done.
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {                    
                    //Declaring variables 
                    int rowID = model.rowID;
                    string reason = model.recallReason;
                    DateTime recallDate = model.recallDate;
                    int visitNo = model.visitNo;
                    bool isRecall = model.isRecall;
                    bool isRowExists = false;
                    bool onSite = model.onSite;

                    connection.Open();
                    string checkForScheduleRowQuery = @"
                    SELECT 
                        CASE 
                            WHEN EXISTS 
                            (
                                SELECT 1 FROM TBL_SERVICE_SCEDULE_UPDATE
                                WHERE T_ID = @rowid
                            )
                        THEN 1 
                        ELSE 0
                    END";
                    isRowExists = connection.QuerySingleOrDefault<bool>(checkForScheduleRowQuery, new { rowid = rowID });
                    if (isRowExists) {
                        string insertRecallQuery = @"
                        INSERT INTO TBL_RECALL_VISIT 
                        (RECALL_REASON, RECALL_DATE, ROW_ID, VISIT_NO, IS_RECALL, IS_COMPLETED, RECALL_TYPE) 
                        OUTPUT INSERTED.RECALL_ID
                        VALUES (@recallReason, @recallDate, @rowId, @visitNo, @isRecall, '0', 'Service')"; 

                        int recallID = connection.ExecuteScalar<int>(insertRecallQuery, new
                        {
                            recallReason = reason,
                            recallDate = recallDate,
                            rowId = rowID,
                            visitNo = visitNo,
                            isRecall = isRecall
                        });                        

                        string updateScheduleVisitQuery = $@"
                        UPDATE TBL_SERVICE_SCEDULE_UPDATE 
                        SET SV{visitNo} = @visitDate, SV{visitNo}_STATUS = @visitStatus, SV{visitNo}_SMS = @visitDate, RECALL_ID = @recallID
                        WHERE T_ID = @rowid";

                        var scheduleVisitResult = connection.Execute(updateScheduleVisitQuery, new
                        {
                            visitDate = GetSriLankanTime(),
                            visitStatus = onSite ? "started" : "pending",
                            rowid = rowID,
                            recallID = recallID,
                        });

                        if (onSite)
                        {
                            string insertActivityQuery = @"
                            INSERT INTO TBL_SCHEDULE_ACTIVITY 
                            (ROW_ID, VISIT_NO, STARTED_BY, STARTED_DATE) 
                            VALUES
                            (@rowid, @visitno, @startedby, @starteddate)";
                            var insertResult = connection.Execute(insertActivityQuery, new { rowid = rowID, visitno = visitNo, startedby = model.techCode, starteddate = GetSriLankanTime()});
                        }                        

                        return Ok("Service Schedule Recalled Successfully.");
                    }
                    else
                    {
                        return NotFound("Service Schedule Row Not Found.");
                    } 
                }                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //Function to get the sri lankan time and date
        private DateTime GetSriLankanTime()
        {
            string[] zoneInfo = { "Asia /Colombo", "Sri Lanka Standard Time" };
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
    }
}
