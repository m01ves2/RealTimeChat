using RealTimeChat.Server.Hubs;

namespace RealTimeChat.Server
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServer(this IServiceCollection services)
        {
            services.AddSingleton<RoomPresence>();

            return services;
        }
    }
}
