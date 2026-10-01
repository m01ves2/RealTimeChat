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

            return services;
        }
    }
}
