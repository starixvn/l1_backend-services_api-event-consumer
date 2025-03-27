namespace ApiEventConsumer.Subscribers.VG
{
    public interface IVGSubscriberService : ISubscriberService
    {
    }

    public class VGSubcriberService : SubscriberService, IVGSubscriberService
    {
        private const string BaseCode = "USD";

        public VGSubcriberService(IVGHighStakeDataAccess highStakeDataAccess, IVGGameConfigDataAccess gameConfigDataAccess, ISentryClient sentryClient)
            : base(BaseCode, highStakeDataAccess, gameConfigDataAccess, sentryClient)
        {
        }
    }
}