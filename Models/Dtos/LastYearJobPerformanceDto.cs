namespace ServvistaWebAppAPI.Models.Dtos
{
    public class LastYearJobPerformanceDto
    {
        public string date { get; set; }
        public int pending { get; set; }
        public int completed { get; set; }
        public int started { get; set; }
        public int cancel { get; set; }
        public int total { get; set; }
    }
}
