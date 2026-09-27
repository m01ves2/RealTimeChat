using RealTimeChat.Domain.Entities;

namespace RealTimeChat.Application.Abstractions
{
    public interface IChatMessageRepository
    {
        Task<IReadOnlyList<ChatMessage>> GetRecentAsync(int roomId, int userId, int quantity, CancellationToken cancellationToken);
        Task AddAsync(ChatMessage chatMessage, CancellationToken cancellationToken);
    }
}
