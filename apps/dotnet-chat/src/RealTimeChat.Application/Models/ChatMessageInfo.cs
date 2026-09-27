namespace RealTimeChat.Application.Models
{
    public sealed record ChatMessageInfo(
        int Id, 
        int RoomId, 
        int AuthorId, 
        string AuthorName, 
        int? RecipientId, 
        string? RecipientName, 
        string Text, 
        DateTime CreatedAt);
}
