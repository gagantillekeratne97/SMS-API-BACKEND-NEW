namespace ServvistaWebAppAPI.Models
{
    public class ServiceVisitUpdateModel
    {
        public string techCode { get; set; }
        public int visitNo { get; set; }
        public string machineRefNo { get; set; }
        public string jobStatus { get; set; }
        public int? meterReadingValue { get; set; }
        public int? hologramNumber { get; set; } 
        public int jobId { get; set; } 
        public string? solution { get; set; }        
    }

    public class ServiceVisitRecallModel
    {
        public int jobId { get; set; }
        public DateTime date { get; set; } 
        public string reason { get; set; }
        public bool isRecall { get; set; } 
        public bool onSite { get; set; }
    }

    public class ServiceScheduleModel
    {
        public string machineRefNo { get; set; }
        public DateTime? exptsv1 { get; set; }
        public DateTime? exptsv2 { get; set; }
        public DateTime? exptsv3 { get; set; }
        public DateTime? exptsv4 { get; set; }
        public DateTime? exptsv5 { get; set; }
        public DateTime? exptsv6 { get; set; }
        public DateTime? sv1 { get; set; }
        public DateTime? sv2 { get; set; }
        public DateTime? sv3 { get; set; }
        public DateTime? sv4 { get; set; }
        public DateTime? sv5 { get; set; }
        public DateTime? sv6 { get; set; }
    }
}
