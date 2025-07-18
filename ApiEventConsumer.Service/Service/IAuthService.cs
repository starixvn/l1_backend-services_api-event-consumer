using System.Net.Http.Headers;

namespace ApiEventConsumer.Service.Service
{
    public interface IAuthService
    {
        /// <summary>
        /// Gets a valid authentication token, either from cache or by requesting a new one
        /// </summary>
        /// <returns>The authentication token or null if failed</returns>
        Task<string?> GetAuthTokenAsync();

        /// <summary>
        /// Sets the authorization header for the provided HttpClient
        /// </summary>
        /// <param name="httpClient">The HttpClient to set the authorization header for</param>
        /// <returns>True if the header was set successfully, false otherwise</returns>
        Task<bool> SetAuthorizationHeaderAsync(HttpClient httpClient);

        /// <summary>
        /// Clears the cached token, forcing a new token request on next call
        /// </summary>
        void ClearCachedToken();
    }
} 