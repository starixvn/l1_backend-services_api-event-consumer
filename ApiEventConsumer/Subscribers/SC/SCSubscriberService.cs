namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCSubscriberService : ISubscriberService
    {
    }

    public class SCSubscriberService : SubscriberService, ISCSubscriberService
    {
        private const string BaseCode = "USD";

        public SCSubscriberService(ISCHighStakeDataAccess highStakeDataAccess, ISCGameConfigDataAccess gameConfigDataAccess, ISentryClient sentryClient)
            : base(BaseCode, highStakeDataAccess, gameConfigDataAccess, sentryClient)
        {
        }
    }
}