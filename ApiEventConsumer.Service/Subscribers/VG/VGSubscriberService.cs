namespace ApiEventConsumer.Subscribers.VG
{
    public interface IVGSubscriberService : ISubscriberService
    {
    }

    public class VGSubcriberService : SubscriberService, IVGSubscriberService
    {
        private const string BaseCode = "USD";

        public VGSubcriberService(
            ISentryClient sentryClient,
            IConfiguration configuration)
            : base(BaseCode, sentryClient, configuration)
        {
        }

        protected override string GetStreamSettingsPath()
        {
            return "AppSettings:SubcriberSettings:VG:StreamSettings";
        }
    }
}