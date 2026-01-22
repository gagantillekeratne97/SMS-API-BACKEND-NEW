using ServvistaWebAppAPI.Models;

namespace ServvistaWebAppAPI.Classes
{
    public interface IServiceSchedule
    {
        Task<List<ServiceVisitDailyInfoModel>> GetTodayServiceVisits(string techCode);         
        Task<int> GetRemainingDays(string techCode, string machineRefNo);
        Task<List<ServiceVisitMonthlyInfo>> GetMonthlyVisits(string techCode);
        Task UpdateServiceSchedule(string techCode, int visitNo, string machineRefno, string jobStatus);
        Task<List<PreviousServiceVisitModel>> GetPreviousServiceVisits(string techCode);        
        Task UpdatePreviousScheduleVisits(string techCode, DateTime visitDate, int visitNo, string machineRefNo, int meterReadingValue);        
    }
}
