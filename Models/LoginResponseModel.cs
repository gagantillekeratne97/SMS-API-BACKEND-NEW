namespace ServvistaWebAppAPI.Models
{
    public class LoginResponseModel
    {
        public string TECH_CODE { get; set; }
        public string TECH_NAME { get; set; } 
        public string MOBILE_NO { get; set; } 
        public string EMAIL { get; set; }
        public string TOKEN { get; set; }
        public string REFRESH_TOKEN { get; set; }
        public string AREA { get; set; } 
        public string CITY { get; set; } 
        public bool IS_ACTIVE { get; set; }
    }
}
