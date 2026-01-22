namespace ServvistaWebAppAPI.Models
{
    public class ServiceVisitDailyInfoModel
    {
        public string cusName { get; set; }
        public string serialNo { get; set; }
        public string machineRefNo { get; set; }
        public string expectedVisitNo { get; set; }
        public DateTime expectedVisitDate { get; set; }
    }

    public class ServiceVisitMonthlyInfo
    {
        public string machineRefNo { get; set; }
        public string expectedVisitNo { get; set; }
        public DateTime expectedVisitDate { get; set; }
        public int expectedVisitCount { get; set; }
        public string VisitStatus { get; set; }
        public int RowId { get; set; } 
        public string customerID  { get; set; }
        public string customerName { get; set; } 
        public string contactPerson { get; set; } 
        public string customerTelephone { get; set; }
        public string machineLocation01 { get; set; }
        public string machineLocation02 { get; set; } 
        public string machineLocation03 { get; set; }   
    }
}
