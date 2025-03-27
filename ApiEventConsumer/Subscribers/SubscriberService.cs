namespace ApiEventConsumer.Subscribers
{
    public interface ISubscriberService
    {
        Task InsertApiEventConsumers(string source, IEnumerable<Providers.HighStakeRecord> ApiEventConsumers);
    }

    public class SubscriberService : ISubscriberService
    {
        protected readonly string baseCode;
        protected readonly IHighStakeDataAccess highStakeDataAccess;
        protected readonly IGameConfigDataAccess gameConfigDataAccess;
        protected readonly ISentryClient sentryClient;

        protected SubscriberService(string baseCode, IHighStakeDataAccess highStakeDataAccess, IGameConfigDataAccess gameConfigDataAccess, ISentryClient sentryClient)
        {
            this.highStakeDataAccess = highStakeDataAccess;
            this.gameConfigDataAccess = gameConfigDataAccess;
            this.baseCode = baseCode;
            this.sentryClient = sentryClient;
        }

        public async Task InsertApiEventConsumers(string source, IEnumerable<Providers.HighStakeRecord> ApiEventConsumers)
        {
            if (ApiEventConsumers.Any())
            {
                var highStakeLog = new ApiEventConsumerLog
                {
                    Time = DateTimeOffset.UtcNow.DateTime,
                    ExDate = ApiEventConsumers.First().HighStakeDate,
                    Source = source,
                    ExId = await GetNextId(),
                    Code = ConvertCurrencyCodes(ApiEventConsumers.Where(ex => ex.BaseCode.Equals(baseCode, StringComparison.OrdinalIgnoreCase)))
                };

                if (highStakeLog.Code.Count > 0)
                {
                    await highStakeDataAccess.Insert(highStakeLog);
                    await gameConfigDataAccess.UpdateReloadHighStake();
                }
                else
                {
                    sentryClient.CaptureMessage($"{DateTime.Now} - High stake got empty codes", SentryLevel.Fatal);
                }
            }
        }

        private async Task<int> GetNextId()
        {
            var latestHighStake = await highStakeDataAccess.GetLastHighStake();

            return latestHighStake == null ? 1 : latestHighStake.ExId + 1;
        }

        private static Dictionary<string, decimal> ConvertCurrencyCodes(IEnumerable<Providers.HighStakeRecord> ApiEventConsumers)
        {
            var codes = new Dictionary<string, decimal>();

            foreach (var rate in ApiEventConsumers)
            {
                if (!string.IsNullOrWhiteSpace(rate.Code))
                {
                    codes.Add(rate.Code, rate.BaseRate);
                }
            }

            return codes;
        }
    }
}