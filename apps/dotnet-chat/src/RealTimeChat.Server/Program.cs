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

    using WebSocket webSocket =
        await context.WebSockets.AcceptWebSocketAsync();

    byte[] buffer = new byte[4 * 1024];

    while (webSocket.State == WebSocketState.Open)
    {
        WebSocketReceiveResult result =
            await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                context.RequestAborted);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(
                result.CloseStatus
                    ?? WebSocketCloseStatus.NormalClosure,
                result.CloseStatusDescription,
                context.RequestAborted);

            break;
        }

        if (result.MessageType != WebSocketMessageType.Text)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.InvalidMessageType,
                "Only text messages are supported.",
                context.RequestAborted);

            break;
        }

        if (!result.EndOfMessage)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Fragmented messages are not supported yet.",
                context.RequestAborted);

            break;
        }

        string message = Encoding.UTF8.GetString(
            buffer,
            0,
            result.Count);

        string echoMessage =
            $"Server received: {message}";

        byte[] echoBytes =
            Encoding.UTF8.GetBytes(echoMessage);

        await webSocket.SendAsync(
            new ArraySegment<byte>(echoBytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            context.RequestAborted);
    }
});

app.Run();