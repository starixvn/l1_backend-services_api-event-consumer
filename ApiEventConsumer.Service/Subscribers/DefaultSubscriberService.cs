using Sentry;

namespace ApiEventConsumer.Subscribers
{
    public class DefaultSubscriberService : SubscriberService
    {
        public DefaultSubscriberService(
            string baseCode,
            ISentryClient sentryClient,
            ILogger<SubscriberService> logger,
            ConfigurationDependencies configurations,
            RepositoryDependencies repositories,
            ServiceDependencies services)
            : base(baseCode, sentryClient, logger, configurations, repositories, services)
        {
        }
    }
} 