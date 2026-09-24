using Microsoft.AspNetCore.Identity;
using RealTimeChat.Application;
using RealTimeChat.Infrastructure;
using RealTimeChat.Server.Hubs;
using RealTimeChat.Server.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme) // выбрать схему по умолчанию
                .AddIdentityCookies(); // и зарегистрировать cookie-обработчики

builder.Services.AddAuthorization();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

app.MapAuthEndpoints();

app.MapHub<ChatHub>("/chat");

app.Run();