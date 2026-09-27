using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealTimeChat.Application.Models;
using RealTimeChat.Application.Services;

namespace RealTimeChat.Server.Hubs
{
    [Authorize]
    public sealed class ChatHub : Hub
    {
        private const string CurrentRoomKey = "CurrentRoomId";
        private readonly ChatService _chatService;
        private readonly RoomPresence _roomPresence;

        public ChatHub(ChatService chatService, RoomPresence roomPresence)
        {
            _chatService = chatService;
            _roomPresence = roomPresence;
        }

        public async Task JoinRoom(int roomId)
        {
            await _chatService.GetRoomAsync(roomId, Context.ConnectionAborted);

            if (!int.TryParse(Context.UserIdentifier, out var userId))
                throw new HubException("User ID is unavailable.");

            var userName = Context.User?.Identity?.Name
                ?? throw new HubException("User name is unavailable.");

            int? previousRoomId =  Context.Items.TryGetValue(CurrentRoomKey, out var value) && value is int id
                    ? id
                    : null;

            if (previousRoomId == roomId)
                return;

            if (previousRoomId is int oldRoomId) {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{oldRoomId}", Context.ConnectionAborted);
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"room:{roomId}",
                Context.ConnectionAborted);

            // Join обновляет существующую запись по ConnectionId.
            _roomPresence.Join(Context.ConnectionId, roomId, userId, userName);
            Context.Items[CurrentRoomKey] = roomId;

            if (previousRoomId is int oldRoom) {
                await Clients.Group($"room:{oldRoom}")
                    .SendAsync("OnlineUsersChanged", oldRoom, _roomPresence.GetUsers(oldRoom));
            }

            await Clients.Group($"room:{roomId}")
                .SendAsync("OnlineUsersChanged", roomId, _roomPresence.GetUsers(roomId));
        }

        public async Task SendMessage(int roomId, string text)
        {
            var message = await SaveMessageAsync(roomId, text, recipientId: null);

            await Clients.Group($"room:{roomId}").SendAsync("ReceiveMessage", message);
        }

        public async Task GetUsers(int roomId)
        {
            var onlineUsers = _roomPresence.GetUsers(roomId);

            await Clients.Group($"room:{roomId}").SendAsync("OnlineUsersChanged", roomId, onlineUsers);
        }

        public async Task SendPrivateMessage(int roomId, string text, int recipientId)
        {
            var message = await SaveMessageAsync(roomId, text, recipientId);

            var userIds = new[] { message.AuthorId.ToString(), recipientId.ToString() }.Distinct().ToArray();

            await Clients.Users(userIds).SendAsync("ReceiveMessage", message);
        }

        public async Task<ChatMessageInfo> SaveMessageAsync(int roomId, string text, int? recipientId)
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
                recipientId,
                Context.ConnectionAborted);
            
            return message;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int? roomId = _roomPresence.Leave(Context.ConnectionId);

            if (roomId is int id) {
                await Clients.Group($"room:{id}").SendAsync("OnlineUsersChanged", id, _roomPresence.GetUsers(id));
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
