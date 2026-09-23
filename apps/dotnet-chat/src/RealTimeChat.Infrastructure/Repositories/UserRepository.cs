using Microsoft.EntityFrameworkCore;
using RealTimeChat.Application.Abstractions;
using RealTimeChat.Application.Models;
using RealTimeChat.Infrastructure.Persistence;

namespace RealTimeChat.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserInfo?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                             .AsNoTracking()
                             .Where(user => user.Id == id)
                             .Select(user => new UserInfo(user.Id, user.UserName!))
                             .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserInfo>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
        {
            if (ids.Count == 0) {
                return [];
            }

            return await _dbContext.Users
                            .AsNoTracking()
                            .Where(user => ids.Contains(user.Id))
                            .Select(user => new UserInfo(user.Id, user.UserName!))
                            .ToListAsync(cancellationToken);
        }
    }
}
