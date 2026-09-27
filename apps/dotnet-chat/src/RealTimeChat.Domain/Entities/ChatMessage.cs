using System.Xml.Linq;

namespace RealTimeChat.Domain.Entities
{
    public sealed class ChatMessage
    {
        public const int MaxTextLength = 500;
        public int Id { get; private set; }
        public int RoomId { get; private set; }
        public int AuthorId { get; private set; }
        public int? RecipientId { get; private set; }
        public string Text { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }

        private ChatMessage()
        {
        }

        public ChatMessage(int roomId, int authorId, string text, int? recipientId)
        {
            if (roomId <= 0) {
                throw new ArgumentOutOfRangeException(nameof(roomId), "Room ID must be positive.");
            }

            if (authorId <= 0) {
                throw new ArgumentOutOfRangeException(nameof(authorId), "Author ID must be positive.");
            }

            if (recipientId is <= 0) { // null means broadcast; a specified recipient ID must be positive
                throw new ArgumentOutOfRangeException(nameof(recipientId), "Recipient ID must be positive.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            text = text.Trim();

            if (text.Length > MaxTextLength) {
                throw new ArgumentException($"Message cannot exceed {MaxTextLength} characters.", nameof(text));
            }

            RoomId = roomId;
            AuthorId = authorId;
            RecipientId = recipientId;
            Text = text;
            CreatedAt = DateTime.UtcNow;
        }

    }
}
