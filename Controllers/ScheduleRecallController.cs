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
        [HttpGet("getAllRecallServices")]
        public IActionResult GetAllRecallServices(string techCode)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    List<RecallResponseModel> recallPreviousScheduleModels = new List<RecallResponseModel>();
                    connection.Open();
                    string query = @"
                    SELECT 
                        ROW_ID              AS RowId,
                        CUS_ID              AS customerID,
                        CUS_NAME            AS customerName,
                        CONTACT_PERSON      AS contactPerson,
                        TEL_NO              AS customerTelephone,
                        M_LOC1              AS machineLocation01,
                        M_LOC2              AS machineLocation02,
                        M_LOC3              AS machineLocation03,
                        MACHINE_REF         AS machineRefNo,
                        SERIAL_NO           AS serialNo,
                        VISIT_NO            AS expectedVisitNo,
                        EXPECTED_VISIT_DATE AS expectedVisitDate,
                        TECH_NAME           AS techName
                    FROM TBL_RECALL_VISIT
                    WHERE TECH_CODE = @techcode";

                    var result = connection.Query<RecallResponseModel>(query, new { techcode = techCode.Trim() }).ToList();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
                    if (isRowExists) 
                    {
                        ////Getting Row info from the TBL_SERVICE_SCEDULE_UPDATE table 
                        ////to Recall 
                        string serviceScheduleTableQuery = $@"
                        SELECT EXPT_SV{visitNo} AS EXPECTED_VISIT_DATE, SERIAL_NO, MACHINE_REF, SS.CUS_ID, SS.CUS_NAME, CUS.CONTACT_PERSON, CUS.TEL_NO
                        M_LOC1, M_LOC2, M_LOC3, SS.TECH_NAME                         
                        FROM TBL_SERVICE_SCEDULE_UPDATE SS
                        INNER JOIN MTBL_CUSTOMER_MASTER CUS
                        ON CUS.CUS_CODE = SS.CUS_ID
                        WHERE SS.T_ID = @rowid";

                        var serviceScheduleResult = connection.QuerySingle(serviceScheduleTableQuery, new { rowid = rowID });

                        string customerCode = serviceScheduleResult.CUS_ID;
                        string customerName = serviceScheduleResult.CUS_NAME;
                        string serialNo = serviceScheduleResult.SERIAL_NO; 
                        string machineRefNo = serviceScheduleResult.MACHINE_REF;
                        string telNo = serviceScheduleResult.TEL_NO;
                        string contactPerson = serviceScheduleResult.CONTACT_PERSON;
                        string machineLoc1 = serviceScheduleResult.M_LOC1; 
                        string machineLoc2 = serviceScheduleResult.M_LOC2; 
                        string machineLoc3 = serviceScheduleResult.M_LOC3; 
                        string techName = serviceScheduleResult.TECH_NAME;
                        DateTime expectedVisitDate = Convert.ToDateTime(serviceScheduleResult.EXPECTED_VISIT_DATE);

                        string insertRecallQuery = @"
                        INSERT INTO TBL_RECALL_VISIT 
                        (RECALL_REASON, RECALL_DATE, ROW_ID, VISIT_NO, IS_RECALL, IS_COMPLETED, RECALL_TYPE, TECH_CODE, CUS_ID, CUS_NAME, 
                        CONTACT_PERSON, TEL_NO, M_LOC1, M_LOC2, M_LOC3, MACHINE_REF, SERIAL_NO, EXPECTED_VISIT_DATE) 
                        OUTPUT INSERTED.RECALL_ID
                        VALUES (@recallReason, @recallDate, @rowId, @visitNo, @isRecall, '0', 'Service', @techcode, @cusid, @cusname, 
                        @contactperson, @telno, @mloc1, @mloc2, @mloc3, @machineref, @serialno, @expectedvisitdate)";

                        int recallID = connection.ExecuteScalar<int>(insertRecallQuery, new
                        {
                            recallReason = reason,
                            recallDate = recallDate,
                            rowId = rowID,
                            visitNo = visitNo,
                            isRecall = isRecall,
                            techcode = model.techCode, 
                            techname = techName,
                            cusid = customerCode, 
                            cusname = customerName, 
                            contactperson = contactPerson, 
                            telno = telNo, 
                            mloc1 = machineLoc1, 
                            mloc2 = machineLoc2, 
                            mloc3 = machineLoc3, 
                            machineref = machineRefNo,
                            serialno = serialNo,      
                            expectedvisitdate = expectedVisitDate,
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
