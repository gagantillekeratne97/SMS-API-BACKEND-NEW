namespace ServvistaWebAppAPI.Models
{
    public class TechnicianPerformanceModel
    {        
        public int TotalJobs { get; set; } 
        public int CompletedJobs { get; set; }  
        public double PerformancePercentage { get; set; }
        public int weeklyCompletedJobs { get; set; }    
    }
}
