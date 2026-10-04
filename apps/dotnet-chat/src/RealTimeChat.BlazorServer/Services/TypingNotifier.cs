namespace RealTimeChat.BlazorServer.Services
{
    //важное отличие: сообщение в ChatNotifier — одноразовое событие.Оно пришло → обработали → всё. Typing — это состояние во времени
    public sealed class TypingNotifier(ILogger<TypingNotifier> logger)
    {
        private readonly object _gate = new();

        private readonly Dictionary<Guid, TypingParticipant> _typing = []; //Он хранит не подписчиков, а состояние: какие участия сейчас печатают.
        public event Func<TypingChange, Task>? TypingChanged;

        public async Task StartTypingAsync(Guid participationId, int roomId, int userId, string userName)
        {
            lock (_gate) { // Сам TypingNotifier будет singleton, а значит к одному _typing могут одновременно обращаться разные Blazor circuits.
                _typing[participationId] = new TypingParticipant(roomId, userId, userName); //только изменение общего состояния
            }

            await NotifyAsync(new TypingChange(participationId, roomId, userId, userName, IsTyping: true)); //Мы не держим lock во время await.!!!
        }

        public async Task StopTypingAsync(Guid participationId)
        {
            TypingParticipant? participant;

            lock (_gate) {
                if (!_typing.Remove(participationId, out participant))
                    return;
            }

            await NotifyAsync(new TypingChange(participationId, participant.RoomId, participant.UserId, participant.UserName, IsTyping: false));
        }

        private async Task NotifyAsync(TypingChange change)
        {
            var handlers = TypingChanged;

            if (handlers is null)
                return;

            foreach (Func<TypingChange, Task> handler in handlers.GetInvocationList()) { //здесь именно последовательный вызов обработчиков
                try {
                    await handler(change);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Failed to publish typing state for user {UserId} in room {RoomId}.", change.UserId, change.RoomId);
                }
            }
        }

        private sealed record TypingParticipant(int RoomId, int UserId, string UserName);
    }
    public sealed record TypingChange(Guid ParticipationId, int RoomId, int UserId, string UserName, bool IsTyping);
}
