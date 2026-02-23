using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ServvistaWebAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class helpdeskController : ControllerBase
    {
        [HttpPost("helpRequest")]
        public async Task<IActionResult> SendEmail([FromBody] HelpDeskVM request)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Help Desk Inquiries", "gagan.tillekeratne97@gmail.com"));
                message.To.Add(MailboxAddress.Parse("gagan.tillekeratne97@gmail.com"));
                message.Subject = "Helpdesk Request.";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <h2>New Help Desk Request</h2>
                        <p><b>Job ID:</b> {request.jobId}</p>
                        <p><b>Problem:</b> {request.problem}</p>
                        <p><b>Mobile Number:</b> {request.mobileNumber}</p>
                        <p><b>Priority:</b> {request.priority}</p>
                    "
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync("gagan.tillekeratne97@gmail.com", "kfdy qemk ndim kdje");
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                return Ok(new { success = true, message = "Test Email Sent Successfully."});
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message); 
            }
        }
    }
}

public class HelpDeskVM
{
    public string jobId { get; set; }
    public string problem {get; set; }
    public string mobileNumber { get; set; } 
    public string priority { get; set; }
}