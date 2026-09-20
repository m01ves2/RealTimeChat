using RealTimeChat.Server.WebSockets;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebSocketConnectionManager>();

var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async (HttpContext context, WebSocketConnectionManager connectionManager) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        return;
    }

    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

    var connection = new WebSocketConnection(webSocket, connectionManager.BroadcastAsync);

    if (!connectionManager.Add(connection)) {
        throw new InvalidOperationException($"Connection {connection.Id} is already registered.");
    }

    try {
        await connection.RunAsync(context.RequestAborted);
    }
    finally {
        connectionManager.Remove(connection.Id);
    }
});

app.Run();