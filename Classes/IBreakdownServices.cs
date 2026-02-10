using ServvistaWebAppAPI.Models;

namespace ServvistaWebAppAPI.Classes
{
    public interface IBreakdownServices
    {
        Task<List<BreakdownModel>> GetCompleteLists(string techCode);
        Task<List<BreakdownModel>> GetPendingLists(string techCode);
        Task<List<BreakdownModel>> GetTodayBreakdownList(string techCode);
        Task UpdateJobStatus(UpdateJobModel model);        
        Task<List<BreakdownModel>> GetTotalBreakdowns(string techCode);
        Task<List<BreakdownCategoriesModel>> GetSolutionCategoriesAsync();
    }
}
