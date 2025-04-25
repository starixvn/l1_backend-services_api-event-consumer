using System.Threading.Tasks;
using NATS.Client;

namespace ApiEventConsumer.Subscribers
{
    public interface ISubscriberService
    {
        // Legacy method for backward compatibility
        Task<bool> HandleBetCreated(object sender, MsgHandlerEventArgs args);
        
        // New methods for the updated pattern
        void RegisterSubscriber(IEventSubscriber subscriber);
        void RegisterAllSubscribers();
        
        // Generic method to get subscriber by type
        T GetSubscriber<T>() where T : class, IEventSubscriber;
    }
} 