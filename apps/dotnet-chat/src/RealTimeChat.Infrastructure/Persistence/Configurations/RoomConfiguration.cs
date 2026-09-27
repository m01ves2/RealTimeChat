using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealTimeChat.Domain.Entities;

namespace RealTimeChat.Infrastructure.Persistence.Configurations
{
    public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.ToTable("Rooms");

            builder.HasKey(room => room.Id);

            builder.Property(room => room.Id)
                .ValueGeneratedOnAdd();

            builder.Property(room => room.Name)
                .IsRequired()
                .HasMaxLength(Room.MaxNameLength);

            builder.HasIndex(room => room.Name)
                .IsUnique();


            // Используем анонимные объекты, потому что у Room.Id private setter, а EF seed требует заранее определённые стабильные ID
            builder.HasData(
                            new
                            {
                                Id = 1,
                                Name = "General"
                            },
                            new
                            {
                                Id = 2,
                                Name = "Programming"
                            },
                            new
                            {
                                Id = 3,
                                Name = "Gaming"
                            },
                            new
                            {
                                Id = 4,
                                Name = "Off Topic"
                            });
        }
    }
}
