namespace ServvistaWebAppAPI.Models
{
    public class PreviousServiceVisitModel
    {
        public int transactionID { get; set; }
        public string techCode { get; set; }
        public string serialNo { get; set; }
        public string machineRefNo { get; set; }
        public DateTime? sv1 { get; set; }
        public DateTime? sv2 { get; set; }
        public DateTime? sv3 { get; set; }
        public DateTime? sv4 { get; set; }
        public DateTime? sv5 { get; set; } 
        public DateTime? sv6 { get; set; }
        public bool? isActive { get; set; }
    }
}
