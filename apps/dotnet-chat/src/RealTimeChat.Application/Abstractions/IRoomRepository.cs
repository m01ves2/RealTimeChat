using RealTimeChat.Domain.Entities;

namespace RealTimeChat.Application.Abstractions
{
    public interface IRoomRepository
    {
        Task<IReadOnlyList<Room>> GetAll(CancellationToken cancellationToken);
        Task<Room?> GetById(int id, CancellationToken cancellationToken);
    }
}
