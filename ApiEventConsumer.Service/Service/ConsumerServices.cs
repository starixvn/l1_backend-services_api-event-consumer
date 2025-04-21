using ApiEventConsumer.Subscribers;
using Sentry;

namespace ApiEventConsumer.Service.Service
{
    public class ConsumerServices : SubscriberServices
    {
        public ConsumerServices(
            ISentryClient sentryClient,
            ConfigurationServices configurations,
            RepositoryServices repositories)
            : base(sentryClient, configurations, repositories)
        {
        }
    }
}