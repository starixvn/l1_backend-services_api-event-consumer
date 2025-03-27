namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNSubscriberService : ISubscriberService
    {
    }

    public class SNSubscriberService : SubscriberService, ISNSubscriberService
    {
        private const string BaseCode = "USD";

        public SNSubscriberService(ISNHighStakeDataAccess highStakeDataAccess, ISNGameConfigDataAccess gameConfigDataAccess, ISentryClient sentryClient)
            : base(BaseCode, highStakeDataAccess, gameConfigDataAccess, sentryClient)
        {
        }
    }
}