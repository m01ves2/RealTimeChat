using RealTimeChat.Contracts.Users;
using System.Collections.Concurrent;

namespace RealTimeChat.Server.Hubs
{
    public class RoomPresence
    {
        private readonly ConcurrentDictionary<string, (int roomId, int userId, string userName)> _roomParticipants = new();

        public void Join(string connectionId, int roomId, int userId, string userName)
        {
            _roomParticipants[connectionId] = (roomId, userId, userName);
        }

        public int? Leave(string connectionId)
        {
            return _roomParticipants.TryRemove(connectionId, out var participant) ? participant.roomId : null;
        }

        public OnlineUserResponse[] GetUsers(int roomId)
        {
            var participants = _roomParticipants.Values.Where(p => p.roomId == roomId)
                                                        .Select(p => (p.userId, p.userName))
                                                        .DistinctBy(p => p.userId);

            return participants.Select(p => new OnlineUserResponse(p.userId, p.userName)).ToArray();
        }
    }
}
