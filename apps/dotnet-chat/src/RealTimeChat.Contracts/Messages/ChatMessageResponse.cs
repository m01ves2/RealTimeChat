namespace RealTimeChat.Contracts.Messages
{
    public sealed record ChatMessageResponse(
        int Id,
        int RoomId,
        int AuthorId,
        string AuthorName,
        int? RecipientId,
        string? RecipientName,
        string Text,
        DateTime CreatedAt);
}
