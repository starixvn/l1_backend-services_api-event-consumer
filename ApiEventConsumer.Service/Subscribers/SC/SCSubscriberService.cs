namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCSubscriberService : ISubscriberService
    {
    }

    public class SCSubscriberService : SubscriberService, ISCSubscriberService
    {
        private const string BaseCode = "USD";

        public SCSubscriberService(
            ISentryClient sentryClient,
            IConfiguration configuration)
            : base(BaseCode, sentryClient, configuration)
        {
        }

        protected override string GetStreamSettingsPath()
        {
            return "AppSettings:SubcriberSettings:SC:StreamSettings";
        }
    }
}