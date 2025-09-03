using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;

namespace ApiEventConsumer.Services
{
    public class PromotionCacheService : BackgroundService
    {
        private const int IntervalInSeconds = 60;
        private const int CacheExpirationInMinutes = 5; // Cache expiration longer than refresh interval for safety

        private readonly ILogger<PromotionCacheService> logger;
        private readonly IPromotionService promotionService;
        private readonly ISharedCacheService sharedCache;
        private readonly ISentryClient sentryClient;

        public PromotionCacheService(
            ILogger<PromotionCacheService> logger,
            IPromotionService promotionService, 
            ISharedCacheService sharedCache,
            ISentryClient sentryClient)
        {
            this.logger = logger;
            this.promotionService = promotionService;
            this.sharedCache = sharedCache;
            this.sentryClient = sentryClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshRunningPromotionCache();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while refreshing promotion cache");
                    sentryClient.CaptureException(ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(IntervalInSeconds), stoppingToken);
            }
        }

        private async Task RefreshRunningPromotionCache()
        {
            try
            {
                var promotions = await promotionService.GetRunningPromotionsAsync();

                if (promotions.Any())
                {
                    var cacheKey = $"{CachedKeys.RunningPromotions}";
                    sharedCache.Set(cacheKey, promotions, TimeSpan.FromMinutes(CacheExpirationInMinutes));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception occurred while refreshing promotion cache");
                sentryClient.CaptureException(ex);
            }
        }

        public static string GetCacheKeyRunningPromotions()
        {
            return $"{CachedKeys.RunningPromotions}";
        }
    }
}
