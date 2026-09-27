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
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString)); //Регистрирует EF-контекст и настраивает PostgreSQL.
                                                                                                         //По умолчанию контекст имеет scoped lifetime: для HTTP-запроса создаётся свой экземпляр.

            services.AddIdentityCore<ApplicationUser>() //Регистрирует основные службы Identity для нашего типа пользователя, прежде всего UserManager<ApplicationUser>.
                                                        //Само по себе подключение к базе или cookie эта строка не создаёт.
                    .AddEntityFrameworkStores<ApplicationDbContext>() //Даёт Identity реализацию хранилища пользователей через EF Core. Именно это мост между UserManager и ApplicationDbContext.
                    .AddSignInManager();  //Регистрирует SignInManager<ApplicationUser> отдельно от UserManager.


            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
