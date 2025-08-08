using System.Net.Http.Headers;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Constants;

namespace ApiEventConsumer.Services
{
    public class AuthService(HttpClient httpClient, IAppSettings appSettings, ISentryClient sentryClient) : IAuthService
    {
        private const int TokenExpiryBufferSeconds = 300; // 5 minutes buffer for token expiry

        private readonly HttpClient httpClient = httpClient;
        private readonly IAppSettings appSettings = appSettings;
        private readonly ISentryClient sentryClient = sentryClient;
        private string? cachedToken;
        private DateTime tokenExpiry = DateTime.MinValue;

        public async Task<string?> GetAuthTokenAsync()
        {
            try
            {
                if (appSettings.AuthApiSettings == null ||
                   string.IsNullOrWhiteSpace(appSettings.AuthApiSettings.BaseUrl) ||
                   string.IsNullOrWhiteSpace(appSettings.AuthApiSettings.ClientId) ||
                   string.IsNullOrWhiteSpace(appSettings.AuthApiSettings.ClientSecret))
                {
                    throw new InvalidOperationException("AuthApiSettings are not properly configured");
                }

                if (!string.IsNullOrEmpty(cachedToken) && DateTime.UtcNow < tokenExpiry)
                {
                    return cachedToken;
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
                    tokenExpiry = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn - TokenExpiryBufferSeconds);
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

        public async Task<bool> SetAuthorizationHeaderAsync(HttpClient httpClient)
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return true;
        }

        public void ClearCachedToken()
        {
            cachedToken = null;
            tokenExpiry = DateTime.MinValue;
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
            return result ?? throw new InvalidOperationException(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
        }
    }
}