using System.Net.Http.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Constants;

namespace ApiEventConsumer.Services
{
    public interface IPromotionService
    {
        Task<UpdateTotalTurnoverResponse> UpdateTotalTurnoverAsync(UpdateTotalTurnoverRequest request);
        Task<List<Promotion>> GetRunningPromotionsAsync();
    }

    public class PromotionService : IPromotionService
    {
        private readonly HttpClient httpClient;
        private readonly IAppSettings appSettings;
        private readonly ISentryClient sentryClient;

        public PromotionService(HttpClient httpClient, IAppSettings appSettings, ISentryClient sentryClient)
        {
            this.httpClient = httpClient;
            this.appSettings = appSettings;
            this.sentryClient = sentryClient;
        }

        public async Task<UpdateTotalTurnoverResponse> UpdateTotalTurnoverAsync(UpdateTotalTurnoverRequest request)
        {
            try
            {
                if (appSettings.PromotionApiSettings == null ||
                    string.IsNullOrWhiteSpace(appSettings.PromotionApiSettings.BaseUrl) ||
                    string.IsNullOrWhiteSpace(appSettings.PromotionApiSettings.Endpoints?.UpdateTotalTurnover))
                {
                    throw new InvalidOperationException("PromotionApiSettings, BaseUrl, or UpdateTotalTurnover endpoint is not configured");
                }

                var url = new Uri(appSettings.PromotionApiSettings.BaseUrl + appSettings.PromotionApiSettings.Endpoints.UpdateTotalTurnover);
                var result = await PostAsync<UpdateTotalTurnoverRequest, UpdateTotalTurnoverResponse>(url, request);
                return result;
            }
            catch (Exception ex)
            {
                return new UpdateTotalTurnoverResponse
                {
                    Success = false,
                    ErrorCode = "EXCEPTION",
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<List<Promotion>> GetRunningPromotionsAsync()
        {
            try
            {
                if (appSettings.PromotionApiSettings == null ||
                    string.IsNullOrWhiteSpace(appSettings.PromotionApiSettings.BaseUrl) ||
                    string.IsNullOrWhiteSpace(appSettings.PromotionApiSettings.Endpoints?.GetRunningPromotions))
                {
                    throw new InvalidOperationException("PromotionApiSettings, BaseUrl, or GetRunningPromotions endpoint is not configured");
                }

                var url = new Uri($"{appSettings.PromotionApiSettings.BaseUrl}{appSettings.PromotionApiSettings.Endpoints.GetRunningPromotions}");
                var result = await GetAsync<List<Promotion>>(url);
                return result;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return [];
            }
        }

        private async Task<TResponse> GetAsync<TResponse>(Uri url)
        {
            var timeout = TimeSpan.FromSeconds(appSettings.PromotionApiSettings?.TimeoutInSeconds ?? 10);
            using var cts = new CancellationTokenSource(timeout);

            using var response = await httpClient.GetAsync(url, cts.Token);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
            return result ?? throw new InvalidOperationException(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(Uri url, TRequest request)
        {
            var timeout = TimeSpan.FromSeconds(appSettings.PromotionApiSettings?.TimeoutInSeconds ?? 10);
            using var cts = new CancellationTokenSource(timeout);

            using var response = await httpClient.PostAsJsonAsync(url, request, cts.Token);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
            return result ?? throw new InvalidOperationException(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
        }
    }
}