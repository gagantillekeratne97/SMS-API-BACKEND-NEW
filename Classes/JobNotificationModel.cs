namespace ServvistaWebAppAPI.Classes
{
    public class JobNotificationModel
    {
        public int jobId { get; set; }
        public DateTime jobDate { get; set; }
        public string serialNo { get; set; }    
        public string machineRefNo { get; set; }
        public string note {  get; set; }
        public string customerName { get; set; }
        public string jobStatus { get; set; }
    }
}
