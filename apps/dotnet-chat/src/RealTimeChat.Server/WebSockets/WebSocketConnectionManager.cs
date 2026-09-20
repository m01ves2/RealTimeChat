using System.Collections.Concurrent;

namespace RealTimeChat.Server.WebSockets
{
    internal sealed class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, WebSocketConnection> _connections = new ConcurrentDictionary<Guid, WebSocketConnection>();

        public bool Add(WebSocketConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            return _connections.TryAdd(connection.Id, connection);
        }

        public bool Remove(Guid connectionId)
        {
            return _connections.TryRemove(connectionId, out _);
        }

        public async Task BroadcastAsync(string message, CancellationToken cancellationToken)
        {
            foreach (var connection in _connections.Values) {
                await connection.EnqueueMessageAsync(message, cancellationToken);
            }
        }
    }
}
