using System.Net.Http.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Constants;

namespace ApiEventConsumer.Service.Service
{
    public class BaseApiService : IBaseApiService
    {
        protected readonly HttpClient httpClient;
        protected readonly IAppSettings appSettings;
        protected readonly ISentryClient sentryClient;
        protected readonly IAuthService authService;

        public BaseApiService(
            HttpClient httpClient, 
            IAppSettings appSettings, 
            ISentryClient sentryClient,
            IAuthService authService)
        {
            this.httpClient = httpClient;
            this.appSettings = appSettings;
            this.sentryClient = sentryClient;
            this.authService = authService;
        }

        /// <summary>
        /// Sends a POST request with JSON content and automatic authentication
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="url">Target URL</param>
        /// <param name="request">Request object</param>
        /// <param name="timeoutInSeconds">Optional timeout in seconds</param>
        /// <param name="requireAuth">Whether authentication is required (default: true)</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> PostAsync<TRequest, TResponse>(
            Uri url, 
            TRequest request, 
            int? timeoutInSeconds = null,
            bool requireAuth = true)
        {
            try
            {
                // Set authorization header if required
                if (requireAuth)
                {
                    var authSuccess = await authService.SetAuthorizationHeaderAsync(httpClient);
                    if (!authSuccess)
                    {
                        throw new InvalidOperationException(ApiServiceConstants.FailedToSetAuthorizationHeader);
                    }
                }

                var timeout = TimeSpan.FromSeconds(timeoutInSeconds ?? 30);
                using var cts = new CancellationTokenSource(timeout);

                using var response = await httpClient.PostAsJsonAsync(url, request, cts.Token);
                
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
                return result ?? throw new InvalidOperationException(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }
        }

        /// <summary>
        /// Sends a GET request with automatic authentication
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="url">Target URL</param>
        /// <param name="timeoutInSeconds">Optional timeout in seconds</param>
        /// <param name="requireAuth">Whether authentication is required (default: true)</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> GetAsync<TResponse>(
            Uri url, 
            int? timeoutInSeconds = null,
            bool requireAuth = true)
        {
            try
            {
                // Set authorization header if required
                if (requireAuth)
                {
                    var authSuccess = await authService.SetAuthorizationHeaderAsync(httpClient);
                    if (!authSuccess)
                    {
                        throw new InvalidOperationException(ApiServiceConstants.FailedToSetAuthorizationHeader);
                    }
                }

                var timeout = TimeSpan.FromSeconds(timeoutInSeconds ?? 30);
                using var cts = new CancellationTokenSource(timeout);

                using var response = await httpClient.GetAsync(url, cts.Token);
                
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
                return result ?? throw new InvalidOperationException(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }
        }

        /// <summary>
        /// Sends a PUT request with JSON content and automatic authentication
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="url">Target URL</param>
        /// <param name="request">Request object</param>
        /// <param name="timeoutInSeconds">Optional timeout in seconds</param>
        /// <param name="requireAuth">Whether authentication is required (default: true)</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> PutAsync<TRequest, TResponse>(
            Uri url, 
            TRequest request, 
            int? timeoutInSeconds = null,
            bool requireAuth = true)
        {
            try
            {
                // Set authorization header if required
                if (requireAuth)
                {
                    var authSuccess = await authService.SetAuthorizationHeaderAsync(httpClient);
                    if (!authSuccess)
                    {
                        throw new InvalidOperationException(ApiServiceConstants.FailedToSetAuthorizationHeader);
                    }
                }

                var timeout = TimeSpan.FromSeconds(timeoutInSeconds ?? 30);
                using var cts = new CancellationTokenSource(timeout);

                using var response = await httpClient.PutAsJsonAsync(url, request, cts.Token);
                
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
                return result ?? throw new InvalidOperationException(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }
        }

        /// <summary>
        /// Sends a DELETE request with automatic authentication
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="url">Target URL</param>
        /// <param name="timeoutInSeconds">Optional timeout in seconds</param>
        /// <param name="requireAuth">Whether authentication is required (default: true)</param>
        /// <returns>Response object</returns>
        public async Task<TResponse> DeleteAsync<TResponse>(
            Uri url, 
            int? timeoutInSeconds = null,
            bool requireAuth = true)
        {
            try
            {
                // Set authorization header if required
                if (requireAuth)
                {
                    var authSuccess = await authService.SetAuthorizationHeaderAsync(httpClient);
                    if (!authSuccess)
                    {
                        throw new InvalidOperationException(ApiServiceConstants.FailedToSetAuthorizationHeader);
                    }
                }

                var timeout = TimeSpan.FromSeconds(timeoutInSeconds ?? 30);
                using var cts = new CancellationTokenSource(timeout);

                using var response = await httpClient.DeleteAsync(url, cts.Token);
                
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cts.Token);
                return result ?? throw new InvalidOperationException(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }
        }
    }
} 