using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using RealTimeChat.Application;
using RealTimeChat.Infrastructure;
using RealTimeChat.Server.Endpoints;
using RealTimeChat.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme) // выбрать схему по умолчанию
                .AddIdentityCookies(); // и зарегистрировать cookie-обработчики

builder.Services.AddAuthorization();

//builder.Services.AddSignalR();
builder.Services.AddSignalR()
    .AddHubOptions<ChatHub>(options =>
        options.AddFilter<ChatHubExceptionFilter>()); // Add exception handling for SignalR

var app = builder.Build();
app.UseBlazorFrameworkFiles(); //добавляем возможность раздавать файлы WASM самим сервером!
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.MapRoomEndpoints();

app.MapHub<ChatHub>("/chat"); //Create ChatHub

app.MapFallbackToFile("index.html"); // раздача файла WASM
app.Run();