using RealTimeChat.Application.Abstractions;
using RealTimeChat.Application.Exceptions;
using RealTimeChat.Application.Models;
using RealTimeChat.Domain.Entities;

namespace RealTimeChat.Application.Services
{
    public class ChatService
    {
        private const int RecentMessageCount = 20;
        private readonly IRoomRepository _roomRepository;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly IUserRepository _userRepository;

        public ChatService(IRoomRepository roomRepository, IChatMessageRepository chatMessageRepository, IUserRepository userRepository)
        {
            _roomRepository = roomRepository;
            _chatMessageRepository = chatMessageRepository;
            _userRepository = userRepository;
        }

        public async Task<IReadOnlyList<RoomInfo>> GetRoomsAsync(CancellationToken cancellationToken)
        {
            var rooms = await _roomRepository.GetAllAsync(cancellationToken);

            var roomInfos = rooms.Select(r => new RoomInfo(r.Id, r.Name)).ToList();

            return roomInfos;
        }

        public async Task<RoomInfo> GetRoomAsync(int roomId, CancellationToken cancellationToken)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room is null) {
                throw new NotFoundException("Room", roomId);
            }

            var roomInfo = new RoomInfo(room.Id, room.Name);

            return roomInfo;
        }

        public async Task<IReadOnlyList<ChatMessageInfo>> GetRecentMessagesAsync(int roomId, int userId, CancellationToken cancellationToken)
        {
            var chatMessages = await _chatMessageRepository.GetRecentAsync(roomId, userId, RecentMessageCount, cancellationToken);

            var authorIds = chatMessages.Select(m => m.AuthorId);
            var recipientIds = chatMessages.Where(m => m.RecipientId.HasValue).Select(m => m.RecipientId!.Value);
            var userIds = authorIds.Union(recipientIds).ToArray();

            
            var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
            var usersById = users.ToDictionary(user => user.Id);

            var result = new List<ChatMessageInfo>(chatMessages.Count);

            foreach (var chatMessage in chatMessages) {
                if (!usersById.TryGetValue(chatMessage.AuthorId, out var author)) {
                    throw new InvalidOperationException($"Author {chatMessage.AuthorId} was not found.");
                }

                string? recipientName = null;
                if (chatMessage.RecipientId is int recipientId) {
                    if (!usersById.TryGetValue(recipientId, out var recipient)) {
                        throw new InvalidOperationException($"Recipient {recipientId} was not found.");
                    }

                    recipientName = recipient.UserName;
                }

                result.Add(new ChatMessageInfo(
                    chatMessage.Id,
                    chatMessage.RoomId,
                    chatMessage.AuthorId,
                    author.UserName,
                    chatMessage.RecipientId,
                    recipientName,
                    chatMessage.Text,
                    chatMessage.CreatedAt));
            }

            return result;
        }

        public async Task<ChatMessageInfo> SendMessageAsync(int roomId, int authorId, string text, int? recipientId, CancellationToken cancellationToken)
        {

            var author = await _userRepository.GetByIdAsync(authorId, cancellationToken);         
            if(author is null) {
                throw new NotFoundException("Author", authorId);

            }

            string? recipientName = null;
            if (recipientId is not null) {
                var recipient = await _userRepository.GetByIdAsync((int)recipientId, cancellationToken);
                if (recipient is null) {
                    throw new NotFoundException("Recipient", recipientId);
                }

                recipientName = recipient.UserName;
            }

            var chatMessage = new ChatMessage(roomId, authorId, text, recipientId);
            await _chatMessageRepository.AddAsync(chatMessage, cancellationToken);

            return new ChatMessageInfo(chatMessage.Id, 
                chatMessage.RoomId, 
                chatMessage.AuthorId, 
                author.UserName, 
                chatMessage.RecipientId, 
                recipientName, 
                chatMessage.Text, 
                chatMessage.CreatedAt);
        }
    }
}
