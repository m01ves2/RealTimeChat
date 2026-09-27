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

            var previousRoomId = GetCurrentRoomId();
            if (previousRoomId == roomId)
                return;

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            await MoveConnectionToRoomGroupAsync(previousRoomId, roomId);

            _roomPresence.Join(Context.ConnectionId, roomId, userId, userName);
            Context.Items[CurrentRoomKey] = roomId;

            await PublishRoomChangeAsync(previousRoomId, roomId);
        }

        private int? GetCurrentRoomId()
        {
            return Context.Items.TryGetValue(CurrentRoomKey, out var value)
                && value is int roomId
                    ? roomId
                    : null;
        }

        private int GetCurrentUserId()
        {
            if (!int.TryParse(Context.UserIdentifier, out var userId))
                throw new HubException("User ID is unavailable.");

            return userId;
        }

        private string GetCurrentUserName()
        {
            return Context.User?.Identity?.Name
                ?? throw new HubException("User name is unavailable.");
        }

        private async Task MoveConnectionToRoomGroupAsync(int? previousRoomId, int roomId)
        {
            if (previousRoomId is int oldRoomId) {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{oldRoomId}", Context.ConnectionAborted);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}", Context.ConnectionAborted);
        }

        private async Task PublishRoomChangeAsync(int? previousRoomId, int roomId)
        {
            if (previousRoomId is int oldRoomId)
                await PublishOnlineUsersAsync(oldRoomId);

            await PublishOnlineUsersAsync(roomId);
        }

        public async Task SendMessage(int roomId, string text)
        {
            var message = await SaveMessageAsync(roomId, text, recipientId: null);

            await Clients.Group($"room:{roomId}").SendAsync("ReceiveMessage", message);
        }

        private Task PublishOnlineUsersAsync(int roomId)
        {
            return Clients.Group($"room:{roomId}").SendAsync("OnlineUsersChanged", roomId, _roomPresence.GetUsers(roomId));
        }

        public async Task SendPrivateMessage(int roomId, string text, int recipientId)
        {
            var message = await SaveMessageAsync(roomId, text, recipientId);

            var userIds = new[] { message.AuthorId.ToString(), recipientId.ToString() }.Distinct().ToArray();

            await Clients.Users(userIds).SendAsync("ReceiveMessage", message);
        }

        private async Task<ChatMessageInfo> SaveMessageAsync(int roomId, string text, int? recipientId)
        {
            var authorId = GetCurrentUserId();
            EnsureJoinedRoom(roomId);

            await _chatService.GetRoomAsync(roomId, Context.ConnectionAborted);

            return await _chatService.SendMessageAsync(
                roomId,
                authorId,
                text,
                recipientId,
                Context.ConnectionAborted);
        }

        private void EnsureJoinedRoom(int roomId)
        {
            if (GetCurrentRoomId() != roomId)
                throw new HubException("Join the room before sending a message.");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int? roomId = _roomPresence.Leave(Context.ConnectionId);

            if (roomId is int id) {
                await PublishOnlineUsersAsync(id);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
