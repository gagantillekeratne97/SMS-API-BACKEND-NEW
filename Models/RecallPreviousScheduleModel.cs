namespace ServvistaWebAppAPI.Models
{
    public class RecallPreviousScheduleModel
    {        
        public string techCode { get; set; }
        public string recallReason { get; set; }
        public DateTime recallDate { get; set; }
        public int rowID { get; set; }
        public int visitNo { get; set; }
        public bool isRecall { get; set; }
        public bool onSite { get; set; }
    }
}

public class RecallResponseModel
{
    public string jobStatus { get; set; }     
    public string techCode { get; set; }
    public string serialNo { get; set; }
    public string machineRefNo { get; set; }
    public string expectedVisitNo { get; set; }
    public DateTime expectedVisitDate { get; set; }    
    public int RowId { get; set; }
    public string customerID { get; set; }
    public string customerName { get; set; }
    public string contactPerson { get; set; }
    public string customerTelephone { get; set; }
    public string machineLocation01 { get; set; }
    public string machineLocation02 { get; set; }
    public string machineLocation03 { get; set; }
    public string techName { get; set; }  
    public string recallReason { get; set; } 
    public DateTime recallDate { get; set; } 
    public string serviceStatus { get; set; }
}
