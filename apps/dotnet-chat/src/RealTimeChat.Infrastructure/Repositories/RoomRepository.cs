using Microsoft.EntityFrameworkCore;
using RealTimeChat.Application.Abstractions;
using RealTimeChat.Domain.Entities;
using RealTimeChat.Infrastructure.Persistence;

namespace RealTimeChat.Infrastructure.Repositories
{
    public sealed class RoomRepository : IRoomRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public RoomRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Rooms
                                .AsNoTracking()
                                .OrderBy(room => room.Id)
                                .ToListAsync(cancellationToken);
        }

        public async Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Rooms
                                .AsNoTracking()
                                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }
    }
}
