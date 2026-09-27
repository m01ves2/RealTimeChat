using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealTimeChat.Domain.Entities;
using RealTimeChat.Infrastructure.Identity;

namespace RealTimeChat.Infrastructure.Persistence.Configurations
{
    public sealed class ChatMessageConfiguration
        : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");

            builder.HasKey(message => message.Id);

            builder.Property(message => message.Id)
                .ValueGeneratedOnAdd();

            builder.Property(message => message.Text)
                .IsRequired()
                .HasMaxLength(ChatMessage.MaxTextLength);

            builder.Property(message => message.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.HasOne<Room>()
                .WithMany()
                .HasForeignKey(message => message.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(message => message.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(message => message.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(message => new
            {
                message.RoomId,
                message.CreatedAt
            });
        }
    }
}
