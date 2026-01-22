namespace ServvistaWebAppAPI.Models
{
    public class MTBL_TECH_OFFICERS
    {
        public string COM_ID { get; set; }
        public string TECH_CODE { get; set; }
        public string TECH_NAME { get; set; }
        public string MOBILE_NO { get; set; } 
        public string EMAIL { get; set; }
        public string AREA { get; set; }
        public string CITY { get; set; } 
        public bool IS_ACTIVE { get; set; }
        public string PASSWORD_HASH { get; set; } 
    }
}
