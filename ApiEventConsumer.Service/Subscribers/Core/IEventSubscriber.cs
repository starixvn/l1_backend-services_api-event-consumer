using System;
using System.Threading.Tasks;
using NATS.Client;

namespace ApiEventConsumer.Subscribers
{
    public interface IEventSubscriber
    {
        string SubjectName { get; }
        
        // Handler for received messages
        void OnMessageReceived(object sender, MsgHandlerEventArgs args);
        
        // No longer needed as we'll use ConfigureJetStream instead
        // void Subscribe(IConnection connection);
    }
} 