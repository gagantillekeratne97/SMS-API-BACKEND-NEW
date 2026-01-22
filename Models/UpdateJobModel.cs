namespace ServvistaWebAppAPI.Models
{
    public class UpdateJobModel
    {
        public string techCode { get; set; } 
        public int jobId { get; set; } 
        public string machineRefNo { get; set; } 
        public string serialNo { get; set; } 
        public string jobStatus { get; set; }
        public string Note { get; set; }
    }
}
