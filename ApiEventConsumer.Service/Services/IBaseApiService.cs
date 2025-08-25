namespace ApiEventConsumer.Services
{
    public interface IBaseApiService
    {
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
        Task<TResponse> PostAsync<TRequest, TResponse>(
            Uri url,
            TRequest request,
            int? timeoutInSeconds = null,
            bool requireAuth = true);

        /// <summary>
        /// Sends a GET request with automatic authentication
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="url">Target URL</param>
        /// <param name="timeoutInSeconds">Optional timeout in seconds</param>
        /// <param name="requireAuth">Whether authentication is required (default: true)</param>
        /// <returns>Response object</returns>
        Task<TResponse> GetAsync<TResponse>(
            Uri url,
            int? timeoutInSeconds = null,
            bool requireAuth = true);

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
        Task<TResponse> PutAsync<TRequest, TResponse>(
            Uri url,
            TRequest request,
            int? timeoutInSeconds = null,
            bool requireAuth = true);

        /// <summary>
        /// Sends a DELETE request with automatic authentication
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="url">Target URL</param>
        /// <param name="timeoutInSeconds">Optional timeout in seconds</param>
        /// <param name="requireAuth">Whether authentication is required (default: true)</param>
        /// <returns>Response object</returns>
        Task<TResponse> DeleteAsync<TResponse>(
            Uri url,
            int? timeoutInSeconds = null,
            bool requireAuth = true);
    }
}