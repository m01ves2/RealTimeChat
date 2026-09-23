using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using RealTimeChat.Application.Abstractions;
using RealTimeChat.Infrastructure.Identity;
using RealTimeChat.Infrastructure.Persistence;
using RealTimeChat.Infrastructure.Repositories;

namespace RealTimeChat.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

            services.AddIdentityCore<ApplicationUser>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddSignInManager();

            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
