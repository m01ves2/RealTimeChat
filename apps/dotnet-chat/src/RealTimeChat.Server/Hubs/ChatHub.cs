using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealTimeChat.Application.Services;

namespace RealTimeChat.Server.Hubs
{
    [Authorize]
    public sealed class ChatHub : Hub
    {
        private const string CurrentRoomKey = "CurrentRoomId";
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task JoinRoom(int roomId)
        {
            await _chatService.GetRoomAsync(roomId, Context.ConnectionAborted);

            // Context.Items - connection data storage 
            if (Context.Items.TryGetValue(CurrentRoomKey, out var value) && value is int previousRoomId) {
                if (previousRoomId == roomId) {
                    return;
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{previousRoomId}", Context.ConnectionAborted);
            }

            await Groups.AddToGroupAsync( Context.ConnectionId, $"room:{roomId}", Context.ConnectionAborted);

            Context.Items[CurrentRoomKey] = roomId;
        }

        public async Task SendMessage(int roomId, string text)
        {
            if (!int.TryParse(Context.UserIdentifier, out var authorId)) { // Context.UserIdentifier - userId with this connection Context.ConnectionId
                throw new HubException("User ID is unavailable.");
            }

            if (!Context.Items.TryGetValue(CurrentRoomKey, out var value)
                || value is not int currentRoomId
                || currentRoomId != roomId) {
                throw new HubException("Join the room before sending a message.");
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
