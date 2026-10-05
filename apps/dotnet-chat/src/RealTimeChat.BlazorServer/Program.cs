using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using RealTimeChat.Application;
using RealTimeChat.BlazorServer;
using RealTimeChat.BlazorServer.Components;
using RealTimeChat.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

// DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddServer();

// Data protection.
// Для нашего проекта сейчас гораздо важнее другое: чтобы ключи имели постоянное место хранения, которое не исчезнет при новой публикации приложения.
//иначе сценарий будет: 
//пользователь login
//        ↓
//cookie зашифрована Data Protection key A
//        ↓
//redeploy / новый key ring
//        ↓
//приложение уже не знает key A
//        ↓
//старая cookie не расшифровывается
//        ↓
//пользователя выбрасывает из аккаунта
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];

if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath)) {
    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState(); //Подключаем передачу состояния аутентификации компонентам

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
