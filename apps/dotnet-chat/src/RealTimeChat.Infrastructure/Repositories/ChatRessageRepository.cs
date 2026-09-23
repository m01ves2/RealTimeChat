using Microsoft.EntityFrameworkCore;
using RealTimeChat.Application.Abstractions;
using RealTimeChat.Domain.Entities;
using RealTimeChat.Infrastructure.Persistence;

namespace RealTimeChat.Infrastructure.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ChatMessageRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
        {
            _dbContext.ChatMessages.Add(chatMessage);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(int roomId, int userId, int quantity, CancellationToken cancellationToken)
        {
            var messages = await _dbContext.ChatMessages
                                    .AsNoTracking()
                                    .Where( m => m.RoomId == roomId && 
                                            (   m.AuthorId == userId || 
                                                m.RecipientId == null || 
                                                m.RecipientId == userId))
                                    .OrderByDescending(m => m.CreatedAt)
                                    .ThenByDescending(m => m.Id)
                                    .Take(quantity)
                                    .ToListAsync(cancellationToken);
            messages.Reverse();
            return messages;
        }
    }
}
