using ServvistaWebAppAPI.Models;

namespace ServvistaWebAppAPI.Classes
{
    public interface ITechnicianPerformanceService
    {
        Task<TechnicianPerformanceModel> GetPerformanceAsync(string techCode);
    }
}
