using NATS.Client;

namespace ApiEventConsumer.Subscribers
{
    public interface IEventSubscriber
    {
        string SubjectName { get; }
        
        Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args);      
    }
} 