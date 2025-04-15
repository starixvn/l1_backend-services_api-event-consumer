using Sentry;

namespace ApiEventConsumer.Subscribers
{
    public class ConsumerService : SubscriberService
    {
        public ConsumerService(
            ISentryClient sentryClient,
            ILogger<SubscriberService> logger,
            ConfigurationDependencies configurations,
            RepositoryDependencies repositories,
            ServiceDependencies services)
            : base(sentryClient, logger, configurations, repositories, services)
        {
        }
    }
} 