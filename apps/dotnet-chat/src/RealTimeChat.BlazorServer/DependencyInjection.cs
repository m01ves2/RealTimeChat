using Microsoft.AspNetCore.Components.Server.Circuits;
using RealTimeChat.BlazorServer.Services;

namespace RealTimeChat.BlazorServer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServer(this IServiceCollection services)
        {
            services.AddSingleton<ChatNotifier>(); // Singleton нужен, чтобы компоненты разных пользователей подписывались на один объект.
                                                   // Scoped-сервис circuit был бы отдельным для каждой вкладки.

            services.AddSingleton<RoomPresence>();

            // сервисы для отслеживания соединения вкладки клиента
            services.AddScoped<RoomCircuitHandler>(); //Компонент будет получать сервис как RoomCircuitHandler
            services.AddScoped<CircuitHandler>(provider => provider.GetRequiredService<RoomCircuitHandler>()); // Blazor ищет обработчики по типу CircuitHandler
            services.AddSingleton<TypingNotifier>();

            return services;
        }
    }
}
