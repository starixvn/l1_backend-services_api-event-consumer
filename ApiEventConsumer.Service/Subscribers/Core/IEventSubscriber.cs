using System;
using System.Threading.Tasks;
using NATS.Client;

namespace ApiEventConsumer.Subscribers
{
    public interface IEventSubscriber
    {
        string SubjectName { get; }
        
        void OnMessageReceived(object sender, MsgHandlerEventArgs args);      
    }
} 