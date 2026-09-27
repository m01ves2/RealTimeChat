using RealTimeChat.Application.Models;

namespace RealTimeChat.Application.Abstractions
{
    public interface IUserRepository
    {
        Task<UserInfo?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<IReadOnlyList<UserInfo>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
    }
}
