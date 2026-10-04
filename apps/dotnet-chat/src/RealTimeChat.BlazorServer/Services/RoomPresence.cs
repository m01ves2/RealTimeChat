namespace RealTimeChat.BlazorServer.Services
{
    public sealed record OnlineUser(int Id, string UserName);

    public sealed class RoomPresence(ILogger<RoomPresence> logger)
    {
        private readonly object _gate = new();

        private readonly Dictionary<Guid, Participation> _participations = []; //Guid - ID конкретного участия

        public event Func<int, Task>? UsersChanged;

        public IReadOnlyList<OnlineUser> GetUsers(int roomId)
        {
            lock (_gate) { // lock защищает общий словарь: разные circuit могут обращаться к singleton одновременно. Уведомления вызываем за пределами lock, после изменения данных.
                return _participations.Values
                    .Where(item => item.RoomId == roomId)
                    .Select(item => new OnlineUser(item.UserId, item.UserName))
                    .DistinctBy(user => user.Id) // UserId — ID пользователя из БД
                    .OrderBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        public async Task JoinAsync(Guid participationId, int roomId, int userId, string userName)
        {
            Participation? previous;

            lock (_gate) {
                _participations.TryGetValue(participationId, out previous);

                var current = new Participation(roomId, userId, userName);

                if (previous == current)
                    return;

                _participations[participationId] = current;
            }

            // При переходе обновляем и старую комнату, и новую.
            if (previous is not null && previous.RoomId != roomId)
                await NotifyAsync(previous.RoomId);

            await NotifyAsync(roomId);
        }

        public async Task LeaveAsync(Guid participationId)
        {
            Participation? removed;

            lock (_gate) {
                if (!_participations.Remove(participationId, out removed))
                    return;
            }

            await NotifyAsync(removed.RoomId);
        }

        private async Task NotifyAsync(int roomId)
        {
            var handlers = UsersChanged;

            if (handlers is null)
                return;

            foreach (Func<int, Task> handler in handlers.GetInvocationList()) {
                try {
                    await handler(roomId);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Failed to update presence for room {RoomId}.", roomId);
                }
            }
        }

        private sealed record Participation(int RoomId, int UserId, string UserName);
    }
}
