namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNSubscriberService : ISubscriberService
    {
    }

    public class SNSubscriberService : SubscriberService, ISNSubscriberService
    {
        private const string BaseCode = "USD";

        public SNSubscriberService(
            ISentryClient sentryClient,
            IConfiguration configuration)
            : base(BaseCode, sentryClient, configuration)
        {
        }

        protected override string GetStreamSettingsPath()
        {
            return "AppSettings:SubcriberSettings:SN:StreamSettings";
        }
    }
}