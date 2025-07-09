using System.Net.Http.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;

namespace ApiEventConsumer.Service.Service
{
    public interface IPromotionService
    {
        Task<UpdateTotalTurnoverResponse> UpdateTotalTurnoverAsync(UpdateTotalTurnoverRequest request);
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
                sentryClient.CaptureException(ex);

                return new UpdateTotalTurnoverResponse
                {
                    Success = false,
                    Code = "EXCEPTION",
                    Message = $"Exception: {ex.Message}"
                };
            }
        }


        private async Task<TResponse> PostAsync<TRequest, TResponse>(Uri url, TRequest request)
        {
            using var response = await httpClient.PostAsJsonAsync(url, request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result ?? throw new InvalidOperationException($"Failed to deserialize response from {url}");
        }
    }
}