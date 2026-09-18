using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using WebSocket webSocket =
        await context.WebSockets.AcceptWebSocketAsync();

    byte[] buffer = new byte[4 * 1024];

    WebSocketReceiveResult result =
        await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            context.RequestAborted);

    if (result.MessageType == WebSocketMessageType.Close)
    {
        await webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Client closed the connection.",
            context.RequestAborted);

        return;
    }

    if (result.MessageType != WebSocketMessageType.Text)
    {
        await webSocket.CloseAsync(
            WebSocketCloseStatus.InvalidMessageType,
            "Only text messages are supported.",
            context.RequestAborted);

        return;
    }

    string message = Encoding.UTF8.GetString(
        buffer,
        0,
        result.Count);

    string echoMessage = $"Server received: {message}";
    byte[] echoBytes = Encoding.UTF8.GetBytes(echoMessage);

    await webSocket.SendAsync(
        new ArraySegment<byte>(echoBytes),
        WebSocketMessageType.Text,
        endOfMessage: true,
        context.RequestAborted);

    await webSocket.CloseAsync(
        WebSocketCloseStatus.NormalClosure,
        "Echo completed.",
        context.RequestAborted);
});

app.Run();