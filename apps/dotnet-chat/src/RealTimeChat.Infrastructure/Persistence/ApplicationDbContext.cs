using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RealTimeChat.Domain.Entities;
using RealTimeChat.Infrastructure.Identity;

namespace RealTimeChat.Infrastructure.Persistence
{
    public sealed class ApplicationDbContext : IdentityUserContext<ApplicationUser, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Configures the ASP.NET Core Identity user model.

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly); // finds RoomConfiguration and ChatMessageConfiguration
        }
    }
}
