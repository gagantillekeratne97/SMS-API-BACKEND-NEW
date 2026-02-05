namespace ServvistaWebAppAPI.Models
{
    public class customerFeedbackRequestModel
    {
        //Customer Feedback Review Text
        public string feedback { get; set; }
        //Customer Feedback Review Count (1 to 5 scale)
        public int feedbackCount { get; set; }
        //Customer Mobile No    
        public string mobileNo { get; set; }
        //Customer Name
        public string customerName { get; set; }
        //Daily job Job id
        public string jobId { get; set; }
    }
}
