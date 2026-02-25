using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Dapper;
using ServvistaWebAppAPI.Models;
using ServvistaWebAppAPI.Classes;
using System.Threading.Tasks;
using ServvistaWebAppAPI.Services;

namespace ServvistaWebAppAPI.Controllers
{    
    [Route("api/[controller]")]
    [ApiController]
    public class BreakdownController : ControllerBase
    {
        private readonly string connectionString;
        private readonly ITechnicianPerformanceService _performanceService;
        private readonly IBreakdownServices _breakdownServices;
        private readonly ITenantService _tenantService; 

        public BreakdownController(IConfiguration config, ITechnicianPerformanceService performanceService, IBreakdownServices breakdownServices, ITenantService tenantService)
        {            
            _performanceService = performanceService;
            _breakdownServices = breakdownServices;
            _tenantService = tenantService;
        }

        //GET: api/breakdown/solutionCategories
        [Authorize]
        [HttpGet("solutionCategories")]
        public async Task<IActionResult> GetSolutionCategories()
        {
            try
            {                
                var result = await _breakdownServices.GetSolutionCategoriesAsync();
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); 
            }
        }

        //api/breakdown/updatejobstatus        
        [Authorize]
        [HttpPost("updatejobstatus")]
        public async Task<IActionResult> UpdateJobStatus(UpdateJobModel model)
        {
            try
            {
                var result = _breakdownServices.UpdateJobStatus(model);
                return Ok($"Successfully Updated. Job Status : {model.jobStatus}"); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); 
            }            
        }        

        //api/breakdown/getperformance
        [Authorize]
        [HttpGet("getperformance")]
        public async Task<IActionResult> GetPerformance(string techCode)
        {
            var result = await _performanceService.GetPerformanceAsync(techCode);
            return Ok(result); 
        }        

        //api/breakdown/complete 
        [Authorize]
        [HttpGet("complete")]
        public async Task<IActionResult> GetCompleteBreakdownLists(string techCode)
        {
            try
            {
                var result = await _breakdownServices.GetCompleteLists(techCode);
                return Ok(result);                                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }        

        //api/breakdown/todaybreakdown
        [Authorize]
        [HttpGet("todaybreakdown")]
        public async Task<IActionResult> GetTodayBreakdownLists(string techCode)
        {
            try
            {
                var result = await _breakdownServices.GetTodayBreakdownList(techCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //api/breakdown/totalbreakdownjobs?techCode={techcode}
        [Authorize]
        [HttpGet("totalbreakdownjobs")]
        public async Task<IActionResult> GetTotalBreakdownJobs(string techCode)
        {
            try
            {
                var result = await _breakdownServices.GetTotalBreakdowns(techCode);
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //api/breakdown/pending
        [Authorize]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingBreakdown(string techCode)
        {
            try
            {
               var result = await _breakdownServices.GetPendingLists(techCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }            
        }
    }
}
