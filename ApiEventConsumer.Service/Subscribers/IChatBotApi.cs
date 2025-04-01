namespace ApiEventConsumer.Service.Subscribers
{
    public interface IChatBotApi
    {
        Task ForwardMessage(string token, string conversationId, string message);
    }
} 