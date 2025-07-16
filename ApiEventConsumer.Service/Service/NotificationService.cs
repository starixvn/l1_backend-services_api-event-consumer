using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;
using Azure.Core;
using NATS.Client.Internals;
using Sentry;

namespace ApiEventConsumer.Service.Service
{
    public interface INotificationService
    {
        Task<PromotionProgressResponse> SendPromotionProgressAsync(PromotionProgressRequest request);
    }

    public class NotificationService : INotificationService
    {
        private readonly HttpClient httpClient;
        private readonly IAppSettings appSettings;
        private readonly ISentryClient sentryClient;
        private string? cachedToken;
        private DateTime tokenExpiry = DateTime.MinValue;

        public NotificationService(HttpClient httpClient, IAppSettings appSettings, ISentryClient sentryClient)
        {
            this.httpClient = httpClient;
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

                // Get auth token
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new PromotionProgressResponse
                    {
                        Success = false,
                        Code = "AUTH_FAILED",
                        Message = "Failed to obtain authentication token" 
                    };
                }
                sentryClient.CaptureMessage($"token: {token}");

                // Prepare request
                var endpoint = appSettings.NotificationApiSettings.PromotionProgressEndpoint ?? "/api/promotionProgress";
                var url = new Uri(appSettings.NotificationApiSettings.BaseUrl + endpoint);

                // Set authorization header
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Send request
                var result = await PostAsync<PromotionProgressRequest, PromotionProgressResponse>(url, request);
                sentryClient.CaptureMessage($"result notify: {result}");
                return result;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                var endpoint = appSettings.NotificationApiSettings.PromotionProgressEndpoint ?? "/api/promotionProgress";
                var url = new Uri(appSettings.NotificationApiSettings.BaseUrl + endpoint);
                sentryClient.CaptureMessage($"Exception notify: {ex.Message} - url: {url} - request: {request} ");

                return new PromotionProgressResponse
                {
                    Success = false,
                    Code = "EXCEPTION",
                    Message = $"Exception: {ex.Message}"
                };
            }
        }

        private async Task<string?> GetAuthTokenAsync()
        {
            try
            {
                // Check if cached token is still valid
                if (!string.IsNullOrEmpty(cachedToken) && DateTime.UtcNow < tokenExpiry)
                {
                    return cachedToken;
                }

                if (appSettings.AuthApiSettings == null ||
                    string.IsNullOrWhiteSpace(appSettings.AuthApiSettings.BaseUrl) ||
                    string.IsNullOrWhiteSpace(appSettings.AuthApiSettings.ClientId) ||
                    string.IsNullOrWhiteSpace(appSettings.AuthApiSettings.ClientSecret))
                {
                    throw new InvalidOperationException("AuthApiSettings are not properly configured");
                }

                // Prepare auth request using form data (OAuth2 standard)
                var authUrl = new Uri(appSettings.AuthApiSettings.BaseUrl + appSettings.AuthApiSettings.TokenEndpoint);

                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "client_credentials"),
                    new("client_id", appSettings.AuthApiSettings.ClientId),
                    new("client_secret", appSettings.AuthApiSettings.ClientSecret),
                    new("scope", appSettings.AuthApiSettings.Scope ?? "admin-api")
                };

                // Clear any existing auth header for token request
                httpClient.DefaultRequestHeaders.Authorization = null;

                // Request token using form data
                var authResponse = await PostFormAsync<AuthTokenResponse>(authUrl, formData);

                if (!string.IsNullOrEmpty(authResponse.AccessToken))
                {
                    cachedToken = authResponse.AccessToken;
                    // Set expiry with some buffer (subtract 5 minutes for safety)
                    tokenExpiry = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn - 300);
                    return cachedToken;
                }

                return null;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return null;
            }
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(Uri url, TRequest request)
        {
            var timeout = TimeSpan.FromSeconds(appSettings.NotificationApiSettings?.TimeoutInSeconds ?? 30);
            using var cts = new CancellationTokenSource(timeout);

            using var response = await httpClient.PostAsJsonAsync(url, request, cts.Token);
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
            return result ?? throw new InvalidOperationException($"Failed to deserialize response from {url}");
        }

        private async Task<TResponse> PostFormAsync<TResponse>(Uri url, List<KeyValuePair<string, string>> formData)
        {
            var timeout = TimeSpan.FromSeconds(appSettings.AuthApiSettings?.TimeoutInSeconds ?? 30);
            using var cts = new CancellationTokenSource(timeout);

            using var content = new FormUrlEncodedContent(formData);
            using var response = await httpClient.PostAsync(url, content, cts.Token);
            
            response.EnsureSuccessStatusCode();

            // Log raw response for debugging
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken: cts.Token);

            // Deserialize from the string content
            var result = JsonSerializer.Deserialize<TResponse>(responseContent);
            return result ?? throw new InvalidOperationException($"Failed to deserialize response from {url}");
        }
    }
} 