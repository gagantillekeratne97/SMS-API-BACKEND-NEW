using ServvistaWebAppAPI.Models;
using ServvistaWebAppAPI.Models.Dtos;
namespace ServvistaWebAppAPI.Repositories
{
    public interface IManagementRepository
    {
        JobCountAndRateDto GetJobCountAndRate();
        JobCountAndRateDto GetServiceCountAndRate();
        List<BreakdownModel> GetPendingJobs();
        List<ServiceVisitMonthlyInfo> GetPendingServices();
        List<CompleteAndPendingPercentageDto> GetCompleteAndPendingJobPercentage();
        List<CompleteAndPendingPercentageDto> GetCompleteAndPendingServicesPercentage();

        List<LastYearJobPerformanceDto> GetLastWeekJobPerformance();
        List<TechnicianPerformenceDto> GetTechniciansPerformence();

        List<OlderstDueDto> GetOldestDueJobs();
        List<WarrantyDetailsDto> GetWarrantyDetails();
    }
}
