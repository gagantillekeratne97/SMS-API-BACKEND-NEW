using ServvistaWebAppAPI.Models;

namespace ServvistaWebAppAPI.Classes
{
    public interface IServiceSchedule
    {
        Task<List<ServiceVisitDailyInfoModel>> GetTodayServiceVisits(string techCode);
        Task<int> GetRemainingDays(string techCode, string machineRefNo);
        Task<List<ServiceVisitMonthlyInfo>> GetMonthlyVisits(string techCode);
        Task<ScheduleResponse> UpdateServiceSchedule(
                                   int jobID,
                                   string techCode, 
                                   int visitNo, 
                                   string machineRefno, 
                                   string jobStatus, 
                                   int? meterReadingValue, 
                                   int? hologramNumber,     
                                   string solution,
                                   string solutionCategory);
        Task<List<PreviousServiceVisitModel>> GetPreviousServiceVisits(string techCode, string machineRefNo);
        Task<List<ServiceVisitMonthlyInfo>> GetTotalServiceVisits(string techCode);
        Task<List<ServiceVisitMonthlyInfo>> GetDueServiceVisits(string techCode);
        Task UpdateServiceVisitRecall(ServiceVisitRecallModel visitRecallModel);
    }
}
