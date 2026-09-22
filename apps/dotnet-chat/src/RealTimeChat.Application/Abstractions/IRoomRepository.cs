using RealTimeChat.Domain.Entities;

namespace RealTimeChat.Application.Abstractions
{
    public interface IRoomRepository
    {
        Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken);
        Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken);
    }
}
