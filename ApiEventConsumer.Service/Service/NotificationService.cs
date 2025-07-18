using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;
using Sentry;

namespace ApiEventConsumer.Service.Service
{
    public interface INotificationService
    {
        Task<PromotionProgressResponse> SendPromotionProgressAsync(PromotionProgressRequest request);
    }

    public class NotificationService : INotificationService
    {
        private readonly IBaseApiService _baseApiService;
        private readonly IAppSettings appSettings;
        private readonly ISentryClient sentryClient;

        public NotificationService(
            IBaseApiService baseApiService,
            IAppSettings appSettings, 
            ISentryClient sentryClient)
        {
            this._baseApiService = baseApiService;
            this.appSettings = appSettings;
            this.sentryClient = sentryClient;
        }

        public async Task<PromotionProgressResponse> SendPromotionProgressAsync(PromotionProgressRequest request)
        {
            try
            {
                if (appSettings.NotificationApiSettings == null ||
                    string.IsNullOrWhiteSpace(appSettings.NotificationApiSettings.BaseUrl))
                {
                    throw new InvalidOperationException("NotificationApiSettings or BaseUrl is not configured");
                }

                // Prepare request
                var endpoint = appSettings.NotificationApiSettings.PromotionProgressEndpoint ?? "/api/promotionProgress";
                var url = new Uri(appSettings.NotificationApiSettings.BaseUrl + endpoint);

                // Send request using base class method with automatic authentication
                var result = await _baseApiService.PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                    url, 
                    request, 
                    appSettings.NotificationApiSettings.TimeoutInSeconds);
                
                return result;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                
                return new PromotionProgressResponse
                {
                    Success = false,
                    Code = "EXCEPTION",
                    Message = $"Exception: {ex.Message}"
                };
            }
        }
    }
} 