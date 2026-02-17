using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Dapper;
using ServvistaWebAppAPI.Models;
using ServvistaWebAppAPI.Classes;
using System.Diagnostics.Eventing.Reader;

namespace ServvistaWebAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class customerfeedbackController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly JwtTokenService _jwt;

        public customerfeedbackController(IConfiguration config)
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

        //POST : api/customerfeedback/addCustomerReview
        [HttpPost("addCustomerReview")]
        public IActionResult AddCustomerReview([FromBody] customerFeedbackRequestModel model)
        {
            try
            {
                string customerReview = model.review;
                string customerName = model.customerName;
                string customerMobileNo = model.mobileNo;
                int feedbackCount = model.rating;
                string jobID = "";
                string scheduleRowID = "";
                bool isService = false;
                string type = model.type;                

                if (type == "service")
                {
                    //Check if the row id exists in the service schedule table
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        scheduleRowID = model.jobId;
                        connection.Open();
                        bool isRowExists = false;
                        string checkJobExistsQuery = @"
                        IF EXISTS (
                            SELECT 1
                            FROM TBL_SERVICE_SCEDULE_UPDATE
                            WHERE T_ID = @rowID                            
                        )
                            SELECT CAST(1 AS BIT) AS IsExists;
                        ELSE
                            SELECT CAST(0 AS BIT) AS IsExists;
                        ";

                        isRowExists = connection.QuerySingle<bool>(checkJobExistsQuery, new { rowID = scheduleRowID });

                        if (isRowExists == false)
                        {
                            return NotFound("Schedule Visit Not Found.");
                        }

                        //If the service exists, get the serial number and machine reference number
                        string serialNo = "";
                        string machineRefNo = "";

                        string getJobDetailsQuery = @"
                        SELECT SERIAL_NO, MACHINE_REF
                        FROM TBL_SERVICE_SCEDULE_UPDATE
                        WHERE T_ID = @rowid";
                        var serviceDetails = connection.QuerySingleOrDefault(getJobDetailsQuery, new { rowid = scheduleRowID });
                        if (serviceDetails != null)
                        {
                            serialNo = serviceDetails.SERIAL_NO;
                            machineRefNo = serviceDetails.MACHINE_REF;
                        }

                        string insertFeedbackQuery = @"
                        INSERT INTO TBL_SV_CUSTOMER_JOB_FEEDBACKS
                        (COM_ID, FB_DATE, MOBILE_NO, CUS_CODE, CUS_NAME, RATING, FULL_MSG, SERIAL_NO, MACHINE_REF_NO, JOB_ID, CUSTOMER_REVIEW, TYPE, VISIT_NO) 
                        VALUES 
                        ('001', @feedbackDate, @mobileNo, 'N/A', @customerName, @rating, @fullmsg, @serialNo, @machineRefNo, @jobid, @customerreview, @type, @visitno)";
                        var customerFeedInsertResult = connection.Execute(insertFeedbackQuery, new
                        {
                            feedbackDate = GetSriLankanTime(),
                            mobileNo = customerMobileNo,
                            customerName = customerName,
                            rating = feedbackCount,
                            fullmsg = customerReview,
                            serialNo = serialNo,
                            machineRefNo = machineRefNo,
                            jobid = scheduleRowID,
                            customerreview = customerReview,
                            type = type,
                            visitno = model.visitNo
                        });
                        if (customerFeedInsertResult <= 0)
                            return BadRequest("Something Went Wrong please contact IT.");

                        return Ok("Visit Feedback is updated Successfully. Your Feedback is highly Appreciated.");
                    }
                }
                else
                {
                    string serialNo = "";
                    string machineRefNo = "";
                    bool IsJobExists = false;


                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        jobID = model.jobId;
                        connection.Open();
                        string checkJobExistsQuery = @"
                        IF EXISTS (
                            SELECT 1
                            FROM TBL_DAILY_JOBS
                            WHERE DJ_ID = @jobID
                        )
                            SELECT CAST(1 AS BIT) AS IsExists;
                        ELSE
                            SELECT CAST(0 AS BIT) AS IsExists;
                        ";

                        IsJobExists = connection.QuerySingle<bool>(checkJobExistsQuery, new { jobID = jobID });
                        if (IsJobExists)
                        {
                            //If the job exists, get the serial number and machine reference number
                            string getJobDetailsQuery = @"
                        SELECT SERIAL_NO, MACHINE_REF_NO
                        FROM TBL_DAILY_JOBS
                        WHERE DJ_ID = @jobid";
                            var jobDetails = connection.QuerySingleOrDefault(getJobDetailsQuery, new { jobid = jobID });
                            if (jobDetails != null)
                            {
                                serialNo = jobDetails.SERIAL_NO;
                                machineRefNo = jobDetails.MACHINE_REF_NO;
                            }

                            //Insert the customer feedback into the TBL_CUSTOMER_FEEDBACK table
                            string insertFeedbackQuery = @"
                        INSERT INTO TBL_SV_CUSTOMER_JOB_FEEDBACKS
                        (COM_ID, FB_DATE, MOBILE_NO, CUS_CODE, CUS_NAME, RATING, FULL_MSG, SERIAL_NO, MACHINE_REF_NO, JOB_ID, CUSTOMER_REVIEW, TYPE) 
                        VALUES 
                        ('001', @feedbackDate, @mobileNo, 'N/A', @customerName, @rating, @fullmsg, @serialNo, @machineRefNo, @jobid, @customerreview, @type)";
                            var customerFeedInsertResult = connection.Execute(insertFeedbackQuery, new
                            {
                                feedbackDate = GetSriLankanTime(),
                                mobileNo = customerMobileNo,
                                customerName = customerName,
                                rating = feedbackCount,
                                fullmsg = customerReview,
                                serialNo = serialNo,
                                machineRefNo = machineRefNo,
                                jobid = jobID,
                                customerreview = customerReview,
                                type = type                                
                            });
                            if (customerFeedInsertResult <= 0)
                                return BadRequest("Something Went Wrong please contact IT.");
                        }
                        else
                        {
                            return NotFound($"No Job found for this Reference ID : {jobID}");
                        }
                    }
                    return Ok("Your Feedback is Saved and We Highly Appreciated. Thank you.");
                }                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //GET : api/customerfeedback/getJobsWithSerial?serialNo=12345        
        [HttpGet("getJobsWithSerial")]
        public IActionResult GetJobsWithSerial(string serialNo, int jobID) 
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    bool IsMachineExists = false;

                    string isMachineExistsQuery = @"
                    IF EXISTS (
                        SELECT 1
                        FROM TBL_DAILY_JOBS
                        WHERE SERIAL_NO = @serialNo AND DJ_ID = @jobID
                    )
                        SELECT CAST(1 AS BIT) AS IsExists;
                    ELSE
                        SELECT CAST(0 AS BIT) AS IsExists;
                    ";

                    IsMachineExists = connection.QuerySingle<bool>(isMachineExistsQuery, new { serialNo = serialNo, jobID = jobID });

                    if (IsMachineExists)
                    {
                        connection.Open();
                        string query = @"
                        SELECT  
                        dj.DJ_ID,
                        dj.DJ_DATE,
                        dj.SERIAL_NO,
                        dj.MACHINE_REF_NO,
                        dj.CUS_NAME,
                        dj.CUS_ADD1,
                        dj.CUS_ADD2,
                        dj.CUS_ADD3,
                        dj.CUS_CONTACT,
                        dj.CUS_TEL_NO,
                        dj.TEAM_ID,
                        dj.TEAM_NAME,
                        dj.TECH_CODE,
                        toff.TECH_NAME,
                        dj.TECH_MOBILE,
                        dj.MACHINE_MODEL_ID,
                        dj.MACHINE_MODEL_NAME,
                        dj.CUS_STATUS,
                        dj.NOTE,
                        dj.JOB_STATUS
                        FROM TBL_DAILY_JOBS dj
                        INNER JOIN MTBL_TECH_OFFICERS toff
                            ON dj.TECH_CODE = toff.TECH_CODE
                        WHERE dj.SERIAL_NO = @serialno
                          AND dj.DJ_ID = @jobid;
                        ";

                        var result = connection.QuerySingleOrDefault<BreakdownModel>(query, new { serialno = serialNo, jobid = jobID });

                        return Ok(result);
                    }
                    else
                    {
                        return NotFound("No Machine Found. Invalid serial No");
                    }
                }                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); 
            }
        }

        [HttpGet("getServiceByRowID")]
        public IActionResult GetServiceByRowID(string serialNo, int rowId, int visitNo)
        {
            try
            {                
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    bool IsMachineExists = false;

                    string isMachineExistsQuery = @"
                    IF EXISTS (
                        SELECT 1
                        FROM TBL_SERVICE_SCEDULE_UPDATE
                        WHERE SERIAL_NO = @serialNo AND T_ID = @rowID
                    )
                        SELECT CAST(1 AS BIT) AS IsExists;
                    ELSE
                        SELECT CAST(0 AS BIT) AS IsExists;
                    ";

                    IsMachineExists = connection.QuerySingle<bool>(isMachineExistsQuery, new { serialNo = serialNo, rowID = rowId });

                    if (!IsMachineExists)
                    {
                        return NotFound("No Machine Found. Invalid serial No");
                    }

                    connection.Open();
                    string query = @"
                    WITH LatestService AS
                    (
                        SELECT *,
                               ROW_NUMBER() OVER
                               (
                                   PARTITION BY SERIAL_NO
                                   ORDER BY T_ID DESC
                               ) AS rn
                        FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                        WHERE IS_ACTIVE = 1
                    )
                    SELECT
                        s.T_ID                AS rowId,
                        s.CUS_ID              AS customerID,
                        c.CUS_NAME            AS customerName,
                        c.CONTACT_PERSON      AS contactPerson,
                        c.TEL_NO              AS customerTelephone,
                        s.M_LOC1              AS machineLocation01,
                        s.M_LOC2              AS machineLocation02,
                        s.M_LOC3              AS machineLocation03,
                        s.MACHINE_REF         AS machineRefNo,
                        s.SERIAL_NO           AS serialNo,
                        t.TECH_NAME           AS techName,
                        v.VisitNo             AS expectedVisitNo,
                        CONVERT(char(10), v.ExpectedDate, 120) AS expectedVisitDate,
                        CASE
                            WHEN v.ActualVisit IS NOT NULL THEN 'COMPLETED'
                            ELSE 'PENDING'
                        END AS VisitStatus
                    FROM LatestService s
                    INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                        ON c.CUS_CODE = s.CUS_ID
                    INNER JOIN dbo.MTBL_TECH_OFFICERS t
                        ON t.TECH_CODE = s.TECH_CODE
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
                    WHERE s.rn = 1
                      AND s.SERIAL_NO = @serialno
                      AND s.T_ID = @rowid
                      AND v.VisitNo = @visitno
                    ORDER BY v.ExpectedDate, v.VisitNo;
                    ";

                    string expectedVisitNo = "";
                    switch (visitNo)
                    {
                        case 1:
                            expectedVisitNo = "EXPT_SV1";
                            break;
                        case 2:
                            expectedVisitNo = "EXPT_SV2";
                            break;
                        case 3:
                            expectedVisitNo = "EXPT_SV3";
                            break;
                        case 4:
                            expectedVisitNo = "EXPT_SV4";
                            break;
                        case 5:
                            expectedVisitNo = "EXPT_SV5";
                            break;
                        case 6:
                            expectedVisitNo = "EXPT_SV6";
                            break;
                        default:
                            break;
                    }

                    var result = connection.Query<ServiceVisitMonthlyInfo>(query, new { serialno = serialNo, rowid = rowId, visitno = expectedVisitNo});
                    return Ok(result);
                }                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //GET : api/customerfeedback/getBreakdownListsForLastYear?serialNo={serialno}
        [Authorize]
        [HttpGet("getBreakdownListsForLastYear")]
        public IActionResult GetBreakdownForLastYear(string serialNo)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    bool isMachineExists = false;

                    string isMachineExistsQuery = @"
                    IF EXISTS (
                        SELECT 1
                        FROM TBL_DAILY_JOBS
                        WHERE SERIAL_NO = @serialNo
                    )
                        SELECT CAST(1 AS BIT) AS IsExists;
                    ELSE
                        SELECT CAST(0 AS BIT) AS IsExists;
                    ";

                    isMachineExists = connection.QuerySingle<bool>(isMachineExistsQuery, new { serialNo = serialNo});

                    if (!isMachineExists)
                    {
                        return NotFound("No Machine Found. Invalid serial No");
                    }

                    string query = @"
                    SELECT  
                    dj.DJ_ID,
                    dj.DJ_DATE,
                    dj.SERIAL_NO,
                    dj.MACHINE_REF_NO,
                    dj.CUS_NAME,
                    dj.CUS_ADD1,
                    dj.CUS_ADD2,
                    dj.CUS_ADD3,
                    dj.CUS_CONTACT,
                    dj.CUS_TEL_NO,
                    dj.TEAM_ID,
                    dj.TEAM_NAME,
                    dj.TECH_CODE,
                    toff.TECH_NAME,
                    dj.TECH_MOBILE,
                    dj.MACHINE_MODEL_ID,
                    dj.MACHINE_MODEL_NAME,
                    dj.CUS_STATUS,
                    dj.NOTE,
                    dj.JOB_STATUS
                    FROM TBL_DAILY_JOBS dj
                    INNER JOIN MTBL_TECH_OFFICERS toff
                        ON dj.TECH_CODE = toff.TECH_CODE
                    WHERE dj.SERIAL_NO = @serialno
                      AND dj.DJ_DATE >= @lastyearfirstday
                      AND dj.DJ_DATE <= @lastyearlastday;
                    ";

                    var firstDayLastYear = new DateTime(GetSriLankanTime().Year - 1, 1, 1);
                    var lastDayLastYear = new DateTime(GetSriLankanTime().Year - 1, 12, 31);
                    var result = connection.Query<BreakdownModel>(query, new { lastyearfirstday = firstDayLastYear, lastyearlastday = lastDayLastYear, serialno = serialNo}).ToList();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //GET : api/customerfeedback/getServicesWithSerial?serialNo=12345
        [Authorize]
        [HttpGet("getServicesWithSerial")]
        public IActionResult GetServicesWithSerial(string serialNo)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                                                  WITH LatestService AS
                            (
                                SELECT *,
                                       ROW_NUMBER() OVER
                                       (
                                           PARTITION BY SERIAL_NO
                                           ORDER BY T_ID DESC
                                       ) AS rn
                                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE
                                WHERE IS_ACTIVE = 1
                            )
                            SELECT
                                s.T_ID                AS rowId,
                                s.CUS_ID              AS customerID,
                                c.CUS_NAME            AS customerName,
                                c.CONTACT_PERSON      AS contactPerson,
                                c.TEL_NO              AS customerTelephone,
                                s.M_LOC1              AS machineLocation01,
                                s.M_LOC2              AS machineLocation02,
                                s.M_LOC3              AS machineLocation03,
                                s.MACHINE_REF         AS machineRefNo,
                                t.TECH_NAME           AS techName,
                                v.VisitNo             AS expectedVisitNo,
                                CONVERT(char(10), v.ExpectedDate, 120) AS expectedVisitDate,
                                CASE
                                    WHEN v.ActualVisit IS NOT NULL THEN 'COMPLETED'
                                    ELSE 'PENDING'
                                END AS VisitStatus
                            FROM LatestService s
                            INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                                ON c.CUS_CODE = s.CUS_ID
                            INNER JOIN dbo.MTBL_TECH_OFFICERS t
                                ON t.TECH_CODE = s.TECH_CODE
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
                            WHERE s.rn = 1
                              AND s.SERIAL_NO = @serialno
                              AND v.ExpectedDate IS NOT NULL
                            ORDER BY v.ExpectedDate, v.VisitNo;
                            ";

                    var services = connection.Query<ServiceVisitMonthlyInfo>(query, new { serialno = serialNo }).ToList();
                    return Ok(services);
                }                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
