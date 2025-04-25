using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NATS.Client;
using ApiEventConsumer.Subscribers.Bet;
using ApiEventConsumer.Subscribers.User;
using ApiEventConsumer.Service.Service;

namespace ApiEventConsumer.Subscribers
{
    public class SubscriberManager : ISubscriberService
    {
        private readonly INatsConnectionService natsService;
        private readonly List<IEventSubscriber> subscribers = new List<IEventSubscriber>();
        private IConnection? natsConnection;

        public SubscriberManager(INatsConnectionService natsService)
        {
            this.natsService = natsService;
            subscribers = new List<IEventSubscriber>();
            
            // Initialize NATS connection
            natsService.InitializeConnection();
        }

        private void EnsureConnection()
        {
            // This is a workaround since we don't have direct access to the connection
            // In a real implementation, you might want to modify INatsConnectionService
            // to expose the connection property
        }

        public void RegisterSubscriber(IEventSubscriber subscriber)
        {
            subscribers.Add(subscriber);
            
            // We'll need to call Subscribe when the connection is available
            // This might be handled through JetStream configuration
        }

        public void RegisterAllSubscribers()
        {
            // Configure each subscriber with its own subject
            foreach (var subscriber in subscribers)
            {
                if (subscriber is BetSubscriber)
                {
                    natsService.ConfigureJetStream(
                        (sender, args) => { 
                            subscriber.OnMessageReceived(sender, args);
                            return Task.FromResult(true);
                        }, 
                        subscriber.SubjectName);
                }
            }
        }

        // Legacy method for backward compatibility
        public Task<bool> HandleBetCreated(object sender, MsgHandlerEventArgs args)
        {
            // Find bet subscriber via LINQ and delegate to it
            var betSubscriber = subscribers.FirstOrDefault(s => s is BetSubscriber);
            if (betSubscriber != null)
            {
                betSubscriber.OnMessageReceived(sender, args);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        // Generic method to get subscriber by type
        public T GetSubscriber<T>() where T : class, IEventSubscriber
        {
            return subscribers.OfType<T>().FirstOrDefault();
        }
    }
} 