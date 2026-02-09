using Microsoft.AspNetCore.SignalR;
namespace ServvistaWebAppAPI.Classes
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var techCode = httpContext.Request.Query["techCode"].ToString();

            if (!string.IsNullOrEmpty(techCode))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, techCode);
            }

            await base.OnConnectedAsync();
        }
    }
}
