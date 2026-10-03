using RealTimeChat.Application.Models;

namespace RealTimeChat.BlazorServer.Services;

public sealed class ChatNotifier(ILogger<ChatNotifier> logger)
{
    //public event Func<ChatMessageInfo, Task>? MessageReceived;

    private readonly object _gate = new();

    private readonly Dictionary<Guid, Subscriber> _subscribers = []; //Guid - ID конкретной подписки — используем _participationId страницы
    private sealed record Subscriber(int UserId, Func<ChatMessageInfo, Task> Handler); //Позволяет определить, кому доступно сообщение. Для private-сообщений

    public void Subscribe(Guid subscriptionId, int userId, Func<ChatMessageInfo, Task> handler)
    {
        lock (_gate) {
            _subscribers[subscriptionId] = new Subscriber(userId, handler);
        }
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        lock (_gate) { // lock защищает общий словарь от одновременных обращений разных circuit.
            _subscribers.Remove(subscriptionId);
        }
    }

    public async Task PublishMessageAsync(ChatMessageInfo message)
    {
        Subscriber[] recipients;

        lock (_gate) {
            recipients = _subscribers.Values.Where(subscriber =>
                                message.RecipientId is null
                                || subscriber.UserId == message.AuthorId
                                || subscriber.UserId == message.RecipientId)
                            .ToArray();
        }

        foreach (var subscriber in recipients) {
            try {
                await subscriber.Handler(message); // Перебор позволяет дождаться каждого обработчика и отдельно обработать его ошибку.
            }
            catch (Exception exception) {
                // Сбой одной подписки не прерывает доставку остальным.
                // Само сообщение уже сохранено в БД.
                logger.LogError(exception, "Failed to deliver message {MessageId} to user {UserId}.", message.Id, subscriber.UserId);
            }
        }
    }
}