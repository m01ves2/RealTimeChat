using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        return;
    }

    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

    const int BufferSize = 4 * 1024;
    const int MaxMessageSize = 64 * 1024;
    byte[] buffer = new byte[BufferSize];

    while (webSocket.State == WebSocketState.Open)
    {
        using MemoryStream messageStream = new();

        WebSocketReceiveResult result;

        do
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted); //read from webSocket data with size <= BufferSize

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync( result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                                            result.CloseStatusDescription,
                                            context.RequestAborted);
                return;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                await webSocket.CloseAsync( WebSocketCloseStatus.InvalidMessageType,
                                            "Only text messages are supported.",
                                            context.RequestAborted);
                return;
            }

            if (messageStream.Length + result.Count > MaxMessageSize)
            {
                await webSocket.CloseAsync( WebSocketCloseStatus.MessageTooBig,
                                            $"Message size cannot exceed {MaxMessageSize} bytes.",
                                            context.RequestAborted);
                return;
            }

            messageStream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        string message = Encoding.UTF8.GetString(messageStream.ToArray());

        string echoMessage = $"Server received: {message}";

        byte[] echoBytes = Encoding.UTF8.GetBytes(echoMessage);

        await webSocket.SendAsync(  new ArraySegment<byte>(echoBytes),
                                    WebSocketMessageType.Text,
                                    endOfMessage: true,
                                    context.RequestAborted);
    }
});

app.Run();