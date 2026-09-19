using System.Net.WebSockets;
using System.Text;

namespace RealTimeChat.Server.WebSockets
{
    internal sealed class WebSocketConnection
    {
        private readonly WebSocket _socket;

        private const int BufferSize = 4 * 1024;
        private const int MaxMessageSize = 64 * 1024;
        private readonly byte[] _buffer = new byte[BufferSize];

        public WebSocketConnection(WebSocket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (_socket.State == WebSocketState.Open)
            {
                using MemoryStream messageStream = new();

                WebSocketReceiveResult result;

                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(_buffer), cancellationToken); //read from webSocket data portion with size <= BufferSize

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                                                    result.CloseStatusDescription,
                                                    cancellationToken);
                        return;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType,
                                                    "Only text messages are supported.",
                                                    cancellationToken);
                        return;
                    }

                    if (messageStream.Length + result.Count > MaxMessageSize)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.MessageTooBig,
                                                    $"Message size cannot exceed {MaxMessageSize} bytes.",
                                                    cancellationToken);
                        return;
                    }

                    messageStream.Write(_buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                string message = Encoding.UTF8.GetString(messageStream.ToArray());

                string echoMessage = $"Server received: {message}";

                byte[] echoBytes = Encoding.UTF8.GetBytes(echoMessage);

                await _socket.SendAsync(new ArraySegment<byte>(echoBytes),
                                            WebSocketMessageType.Text,
                                            endOfMessage: true,
                                            cancellationToken);
            }
        }


    }
}
