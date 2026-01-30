using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServvistaWebAppAPI.Classes;

namespace ServvistaWebAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    { 
        private readonly IServiceSchedule _serviceSchedule; 
        public ServiceController(IServiceSchedule serviceSchedule, IConfiguration config)
        {        
            _serviceSchedule = serviceSchedule;
        }                
        
        //PUT api/service/updateservicevisit?techCode={techCode}&visitno={visitno}&machinerefno={machinerefno}&jobStatus={jobstatus}
        [Authorize]
        [HttpPost("updateservicevisit")]
        public IActionResult UpdateServiceVisit(string techCode, 
                                                int visitNo, 
                                                string machineRefNo, 
                                                string jobStatus, 
                                                int meterReadingValue, 
                                                int hologramNumber, 
                                                int jobId)
        {
            try
            {
                var result = _serviceSchedule.UpdateServiceSchedule(
                                                       jobId,
                                                       techCode, 
                                                       visitNo, 
                                                       machineRefNo, 
                                                       jobStatus, 
                                                       meterReadingValue, 
                                                       hologramNumber).Result;

                return Ok(new { result.errorMessage, result.statusCode});
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    errorMessage = ex.Message, 
                    statusCode = 500
                }); 
            }
        }

        //GET api/service/getmonthlyservicevisits
        [Authorize]
        [HttpGet("getmonthlyservicevisits")]
        public async Task<IActionResult> GetMonthlyServiceVisits(string techCode)
        {
            try
            {                
                var result = _serviceSchedule.GetMonthlyVisits(techCode).Result;
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //GET api/service/getmonthlyservicevisitscount?techCode={techCode}
        [Authorize]
        [HttpGet("getmonthlyservicevisitscount")]
        public async Task<IActionResult> GetMonthlyServiceVisitsCount(string techCode)
        {
            try
            {
                var serviceVisitsForMonth = _serviceSchedule.GetMonthlyVisits(techCode).Result; 
                var result = serviceVisitsForMonth.Count;
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //GET api/service/totalservicevisits?techCode={techCode}
        [Authorize]
        [HttpGet("totalservicevisits")]
        public async Task<IActionResult> GetTotalServiceVisits(string techCode)
        {
            try
            {
                var result = _serviceSchedule.GetTotalServiceVisits(techCode).Result; 
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //New api endpoints 
        //Service Schedule Recall Endpoints
        [Authorize]
        [HttpGet("alltimedueservices")]
        public async Task<IActionResult> GetAllTimeDueServices(string techCode)
        {
            try
            {
                var result = _serviceSchedule.GetDueServiceVisits(techCode).Result;
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); 
            }
        }

        //GET api/service/getpreviousvisits?techCode={techcode}&machinerefno={machinerefno} 
        [Authorize]
        [HttpGet("previousservicelists")]
        public async Task<IActionResult> GetPreviousVisits(string techCode, string machineRefNo)
        {
            try
            {
                var result = _serviceSchedule.GetPreviousServiceVisits(techCode, machineRefNo).Result; 
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }        

        //GET api/service/getremainingdates?techCode={techCode}&machinerefno={machinerefno}
        [Authorize]
        [HttpGet("getremainingdates")]
        public async Task<IActionResult> GetRemainingDates(string techCode, string machineRefNo)
        {
            try
            {
                var result = _serviceSchedule.GetRemainingDays(techCode, machineRefNo).Result;
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }        

        //GET api/service/gettodayservicevisits?techCode={techCode}
        [Authorize]
        [HttpGet("gettodayservicevisits")]
        public async Task<IActionResult> GetTodayServiceVisits(string techCode)
        {
            try
            {
                var result = _serviceSchedule.GetTodayServiceVisits(techCode).Result;
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
