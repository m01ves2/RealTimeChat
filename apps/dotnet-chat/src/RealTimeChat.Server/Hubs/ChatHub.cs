using Microsoft.AspNetCore.SignalR;

namespace RealTimeChat.Server.Hubs
{
    public sealed class ChatHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
