namespace ServvistaWebAppAPI.Models
{
    public class BreakdownModel
    {
        public string DJ_ID { get; set; } 
        public string SERIAL_NO {  get; set; } 
        public string MACHINE_REF_NO { get; set; }         
        public string CUS_NAME { get; set; }
        public string CUS_ADD1 { get; set; } 
        public string CUS_ADD2 { get; set; } 
        public string CUS_ADD3 { get; set; }     
        public string CUS_CONTACT {  get; set; } 
        public string CUS_TEL_NO { get; set; }
        public string TEAM_ID { get; set; } 
        public string TEAM_NAME { get; set; }
        public DateTime DJ_DATE { get; set; }
        public string TECH_CODE { get; set; } 
        public string TECH_MOBILE { get;set; }
        public string MACHINE_MODEL_ID { get; set; }
        public string MACHINE_MODEL_NAME { get; set; }
        public string CUS_STATUS { get; set; }
        public string NOTE { get; set; }
        public string JOB_STATUS { get; set; }   
    }
}
