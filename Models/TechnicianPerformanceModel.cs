namespace ServvistaWebAppAPI.Models
{
    public class TechnicianPerformanceModel
    {
        public string TechCode { get; set; } 
        public int TotalJobs { get; set; } 
        public int CompletedJobs { get; set; }  
        public double PerformancePercentage { get; set; } 
    }
}
