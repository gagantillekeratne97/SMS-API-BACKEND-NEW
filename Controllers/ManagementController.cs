using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServvistaWebAppAPI.Repositories;
using ServvistaWebAppAPI.Services;

namespace ServvistaWebAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagementController : ControllerBase
    {
        private readonly IManagementRepository _managementService;

        public ManagementController(IManagementRepository managementRepository, ITenantService tenantService)
        {
            _managementService = managementRepository;
        }

        [Authorize]
        [HttpGet("getJobCountAndRate")]
        public IActionResult GetJobCountAndRate()
        {
            try
            {
                // Placeholder for actual logic to retrieve job count and rate
                var data = _managementService.GetJobCountAndRate();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getServiceCountAndRate")]
        public IActionResult GetServiceCountAndRate()
        {
            try
            {
                // Placeholder for actual logic to retrieve job count and rate
                var data = _managementService.GetServiceCountAndRate();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getPendingJobs")]
        public IActionResult GetPendingJobs()
        {
            try
            {
                var data = _managementService.GetPendingJobs();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getPendingServices")]
        public IActionResult GetPendingServices()
        {
            try
            {
                var data = _managementService.GetPendingServices();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getCompleteAndPendingJobPercentage")]
        public IActionResult GetCompleteAndPendingJobPercentage()
        {
            try
            {
                var data = _managementService.GetCompleteAndPendingJobPercentage();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [Authorize]
        [HttpGet("getCompleteAndPendingServicesPercentage")]
        public IActionResult GetCompleteAndPendingServicesPercentage()
        {
            try
            {
                var data = _managementService.GetCompleteAndPendingServicesPercentage();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [Authorize]
        [HttpGet("getLastWeekJobPerformence")]
        public IActionResult GetLastWeekJobPerformence()
        {
            try
            {
                var data = _managementService.GetLastWeekJobPerformance();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getTechniciansPerformence")]
        public IActionResult GetTechniciansPerformence()
        {
            try
            {
                var data = _managementService.GetTechniciansPerformence();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getOldestDueJobs")]
        public IActionResult GetOlderstDueJobs()
        {
            try
            {
                var data = _managementService.GetOldestDueJobs();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [Authorize]
        [HttpGet("getCustomerWarranty")]
        public IActionResult GetWarrantyDetails()
        {
            try
            {
                var data = _managementService.GetWarrantyDetails();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

    }
}
