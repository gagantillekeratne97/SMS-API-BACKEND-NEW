using Microsoft.AspNetCore.SignalR; 
namespace ServvistaWebAppAPI.Classes
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(JobNotificationModel notification)
        {
            await Clients.All.SendAsync("ReceiveNotification", notification);
        }
    }
}
