using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;

namespace ApiEventConsumer.Service.Service
{
    public interface IPromotionService
    {
        Task<UpdateTotalTurnoverResponse> UpdateTotalTurnoverAsync(UpdateTotalTurnoverRequest request);
        Task<HasBonusWinResponse> HasBonusWinAsync(HasBonusWinRequest request);
    }

    public class PromotionService : IPromotionService
    {
        private readonly HttpClient httpClient;
        private readonly IAppSettings appSettings;
        private readonly ISentryClient sentryClient;

        public PromotionService(IHttpClientFactory httpClientFactory, ConfigurationServices configurations, ISentryClient sentryClient)
        {
            this.httpClient = httpClientFactory.CreateClient();
            this.appSettings = configurations.AppSettings;
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
                var responseContent = await PostJsonAsync(url, request);

                var result = JsonSerializer.Deserialize<UpdateTotalTurnoverResponse>(responseContent);
                return result ?? new UpdateTotalTurnoverResponse { Success = false, Code = "ERROR", Message = "Empty response" };
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

        public async Task<HasBonusWinResponse> HasBonusWinAsync(HasBonusWinRequest request)
        {
            try
            {
                if (appSettings.PromotionApiSettings == null || 
                    string.IsNullOrWhiteSpace(appSettings.PromotionApiSettings.BaseUrl) ||
                    string.IsNullOrWhiteSpace(appSettings.PromotionApiSettings.Endpoints?.HasBonusWin))
                {
                    throw new InvalidOperationException("PromotionApiSettings, BaseUrl, or HasBonusWin endpoint is not configured");
                }

                var url = new Uri(appSettings.PromotionApiSettings.BaseUrl + appSettings.PromotionApiSettings.Endpoints.HasBonusWin);
                var responseContent = await PostJsonAsync(url, request);

                var result = JsonSerializer.Deserialize<HasBonusWinResponse>(responseContent);
                return result ?? new HasBonusWinResponse { Success = false, Code = "ERROR", Message = "Empty response" };
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return new HasBonusWinResponse
                {
                    Success = false,
                    Code = "EXCEPTION",
                    Message = $"Exception: {ex.Message}"
                };
            }
        }

        private async Task<string> PostJsonAsync<T>(Uri url, T request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("accept", "application/json");

            var response = await httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP {response.StatusCode}: {responseContent}");
            }

            return responseContent;
        }
    }
} 