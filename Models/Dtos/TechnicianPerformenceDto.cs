namespace ServvistaWebAppAPI.Models.Dtos
{
    public class TechnicianPerformenceDto
    {
        public string tech_id { get; set; }
        public string name { get; set; }
        public int completedJobs { get; set; }
        public double rating { get; set; }
        public int services { get; set; }
        public int breakdowns { get; set; }

    }
}
