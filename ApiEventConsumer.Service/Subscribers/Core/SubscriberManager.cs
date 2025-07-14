using ApiEventConsumer.Service.Service;

namespace ApiEventConsumer.Subscribers
{
    public class SubscriberManager : ISubscriberService
    {
        private readonly INatsConnectionService natsService;
        private readonly List<IEventSubscriber> subscribers;

        public SubscriberManager(INatsConnectionService natsService)
        {
            this.natsService = natsService;
            subscribers = [];
            
            natsService.InitializeConnection();
        }

        public void RegisterSubscriber(IEventSubscriber subscriber)
        {
            subscribers.Add(subscriber);          
        }

        public void RegisterAllSubscribers()
        {
            foreach (var subscriber in subscribers)
            {
                natsService.ConfigureJetStream(subscriber.OnMessageReceived, subscriber.SubjectName);
            }
        }

        public T GetSubscriber<T>() where T : class, IEventSubscriber
        {
            return subscribers.OfType<T>().FirstOrDefault() ?? throw new InvalidOperationException($"No subscriber of type {typeof(T).Name} found.");
        }
    }
} 