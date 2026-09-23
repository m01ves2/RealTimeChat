using RealTimeChat.Infrastructure;
using RealTimeChat.Application;
using RealTimeChat.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);


builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", () => "Hello World!");

app.MapHub<ChatHub>("/chat");

app.Run();