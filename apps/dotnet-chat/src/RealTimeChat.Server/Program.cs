using RealTimeChat.Server.WebSockets;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        return;
    }

    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(); 
    var connection = new WebSocketConnection(webSocket);

    await connection.RunAsync(context.RequestAborted);
});

app.Run();