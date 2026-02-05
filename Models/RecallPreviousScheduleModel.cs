namespace ServvistaWebAppAPI.Models
{
    public class RecallPreviousScheduleModel
    {        
        public string recallReason { get; set; }
        public DateTime recallDate { get; set; }
        public int rowID { get; set; }
        public int visitNo { get; set; }
        public bool isRecall { get; set; }
        public bool onSite { get; set; }
    }
}
