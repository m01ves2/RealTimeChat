using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace RealTimeChat.Server.WebSockets
{
    internal sealed class WebSocketConnection
    {
        public Guid Id { get; } = Guid.NewGuid();
        private readonly WebSocket _socket;
        private readonly Channel<string> _outgoingMessages = Channel.CreateUnbounded<string>();

        private const int BufferSize = 4 * 1024;
        private const int MaxMessageSize = 64 * 1024;
        private readonly byte[] _buffer = new byte[BufferSize];

        private readonly Func<string, CancellationToken, Task> _messageHandler;

        private WebSocketCloseStatus _closeStatus = WebSocketCloseStatus.NormalClosure;
        private string? _closeDescription;

        public WebSocketConnection(WebSocket socket, Func<string, CancellationToken, Task> messageHandler)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // Запускаем оба цикла параллельно.
            // Когда один завершается, отменяем второй и ожидаем полную остановку.
            // После этого завершаем WebSocket close handshake.

            // Create a shared cancellation signal for both loops. Linked with cancellationToken. We can use connectionCts.Cancel()
            using CancellationTokenSource connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start receiving and sending concurrently.
            Task receiveTask = ReceiveLoopAsync(connectionCts.Token);
            Task sendTask = SendLoopAsync(connectionCts.Token);

            // When either loop finishes, ask the other one to stop.
            await Task.WhenAny(receiveTask, sendTask);
            connectionCts.Cancel(); //send Cancel signal to another cicle (inside receiveTask or inside sendTask)

            // Wait until both loops have actually stopped.
            try {
                await Task.WhenAll(receiveTask, sendTask);
            }
            catch (OperationCanceledException) when (connectionCts.IsCancellationRequested) {
                // Expected when the other loop has finished.
                // Coordinated cancellation is expected.
            }

            if ((_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived) &&
                 !cancellationToken.IsCancellationRequested) {
                await _socket.CloseAsync(_closeStatus, _closeDescription, cancellationToken);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try {
                while (_socket.State == WebSocketState.Open) {
                    using MemoryStream messageStream = new();

                    WebSocketReceiveResult result;

                    do {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(_buffer), cancellationToken); //read from webSocket data portion with size <= BufferSize

                        if (result.MessageType == WebSocketMessageType.Close) {
                            _closeStatus = result.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
                            _closeDescription = result.CloseStatusDescription;
                            return;
                        }

                        if (result.MessageType != WebSocketMessageType.Text) {
                            _closeStatus = WebSocketCloseStatus.InvalidMessageType;
                            _closeDescription = "Only text messages are supported.";
                            return;
                        }

                        if (messageStream.Length + result.Count > MaxMessageSize) {
                            _closeStatus = WebSocketCloseStatus.MessageTooBig;
                            _closeDescription = $"Message size cannot exceed {MaxMessageSize} bytes.";
                            return;
                        }

                        messageStream.Write(_buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);
                    string message = Encoding.UTF8.GetString(messageStream.ToArray());

                    await _messageHandler(message, cancellationToken);
                }
            }
            finally {
                _outgoingMessages.Writer.TryComplete();
            }
        }

        private async Task SendLoopAsync(CancellationToken cancellationToken)
        {
            await foreach (string message in _outgoingMessages.Reader.ReadAllAsync(cancellationToken)) {
                
                byte[] echoBytes = Encoding.UTF8.GetBytes(message);

                await _socket.SendAsync(new ArraySegment<byte>(echoBytes),
                                            WebSocketMessageType.Text,
                                            endOfMessage: true,
                                            cancellationToken);
            }
        }

        public async Task EnqueueMessageAsync(string message, CancellationToken cancellationToken)
        {
            await _outgoingMessages.Writer.WriteAsync(message, cancellationToken);
        }
    }
}
