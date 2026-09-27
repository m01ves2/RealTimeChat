using Microsoft.Extensions.DependencyInjection;
using RealTimeChat.Application.Services;

namespace RealTimeChat.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ChatService>();
            return services;
        }
    }
}
