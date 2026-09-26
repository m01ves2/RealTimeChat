using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RealTimeChat.BlazorWasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args); //Создаёт окружение Blazor в браузере: конфигурацию, DI-контейнер и механизм отображения компонентов
builder.RootComponents.Add<App>("#app"); // Находит элемент с id="app" в wwwroot/index.html и помещает туда корневой компонент App. В нём работает маршрутизация: она выбирает, какую страницу показать.
builder.RootComponents.Add<HeadOutlet>("head::after"); // Позволяет компонентам менять содержимое <head> страницы. Например, <PageTitle>Home</PageTitle> устанавливает заголовок вкладки браузера.

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }); // Регистрирует HttpClient для запросов из браузера. Пока его базовый адрес — адрес самого WASM-приложения.

await builder.Build().RunAsync(); // Собирает и запускает клиентское Blazor-приложение. После этого отображается App, а маршрутизатор находит страницу с @page "/".
