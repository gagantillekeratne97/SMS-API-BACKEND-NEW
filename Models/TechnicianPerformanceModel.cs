namespace ServvistaWebAppAPI.Models
{
    public class TechnicianPerformanceModel
    {        
        public int TotalJobs { get; set; } 
        public int CompletedJobs { get; set; }  
        public double jobPerformancePercentage { get; set; }
        public int weeklyCompletedJobs { get; set; }    
        public int lastWeekAllJobs { get; set; }
        public int lastWeekCompletedJobs { get; set; }

        //Service schedule visit properties 
        public int totalServices { get; set; }
        public int completedServices { get; set; }
        public int lastWeekAllServices { get; set; }
        public int lastWeekCompetedServices { get; set; }
        public double servicesPerformancePercentage { get; set; }
    }
}
