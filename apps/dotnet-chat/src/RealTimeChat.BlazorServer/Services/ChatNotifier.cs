using RealTimeChat.Application.Models;

namespace RealTimeChat.BlazorServer.Services;

public sealed class ChatNotifier(ILogger<ChatNotifier> logger)
{
    public event Func<ChatMessageInfo, Task>? MessageReceived;

    public async Task PublishPublicMessageAsync(ChatMessageInfo message)
    {
        // Этот метод предназначен для общей рассылки.
        if (message.RecipientId is not null)
            throw new ArgumentException("Only public messages can be broadcast.", nameof(message));

        var handlers = MessageReceived;

        if (handlers is null)
            return;

        foreach (Func<ChatMessageInfo, Task> handler in handlers.GetInvocationList()) {
            try {
                await handler(message);
            }
            catch (Exception ex) {
                // Сбой одной подписки не прерывает доставку остальным.
                // Само сообщение уже сохранено в БД.
                logger.LogError(ex, "Failed to deliver message {MessageId} to a component.", message.Id);
            }
        }
    }
}