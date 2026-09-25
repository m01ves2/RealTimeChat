using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealTimeChat.Application.Services;

namespace RealTimeChat.Server.Hubs
{
    [Authorize]
    public sealed class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task JoinRoom(int roomId)
        {
            await _chatService.GetRoomAsync(roomId, Context.ConnectionAborted);

            await Groups.AddToGroupAsync( Context.ConnectionId, $"room:{roomId}", Context.ConnectionAborted); 
            //later we can send message to group participants await Clients.Group("room:2").SendAsync("ReceiveMessage", message);
        }

        public async Task SendMessage(int roomId, string text)
        {
            if (!int.TryParse(Context.UserIdentifier, out var authorId)) { // Context.UserIdentifier - userId with this connection Context.ConnectionId
                throw new HubException("User ID is unavailable.");
            }

            await _chatService.GetRoomAsync(roomId, Context.ConnectionAborted);

            var message = await _chatService.SendMessageAsync(
                roomId,
                authorId,
                text,
                recipientId: null,
                Context.ConnectionAborted);

            await Clients.Group($"room:{roomId}").SendAsync("ReceiveMessage", message);
        }
    }
}
