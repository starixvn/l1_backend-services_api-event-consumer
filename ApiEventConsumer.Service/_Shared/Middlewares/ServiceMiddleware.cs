using System.Net;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Subscribers;
using Coravel;
using Refit;

namespace ApiEventConsumer._Shared.Middlewares
{
    public static class ServiceMiddleware
    {
        public static void AddChatBotServices(this IServiceCollection services, AppSettings appSettings)
        {
            if (appSettings.ChatBotApiSettings?.Url == null)
            {
                return;
            }

            HttpClient client = InitHttpClient(appSettings.ChatBotApiSettings.Url, appSettings.ChatBotApiSettings.Proxy ?? string.Empty);
            client.Timeout = TimeSpan.FromSeconds(appSettings.ChatBotApiSettings.TimeoutInSeconds);

            services.AddSingleton(RestService.For<IChatBotApi>(client));
        }

        private static HttpClient InitHttpClient(string url, string proxy)
        {
            HttpClientHandler? handler = null;

            if (!string.IsNullOrEmpty(proxy))
            {
                handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxy),
                    UseProxy = true,
                };
            }

            var client = (handler == null) ? new HttpClient() : new HttpClient(handler);
            client.BaseAddress = new Uri(url);

            return client;
        }
    }
}
