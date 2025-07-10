using ApiEventConsumer._Shared.Middlewares;
using TinyHealthCheck;
using MongoDB.Driver;
using ApiEventConsumer.Service.Subscribers;
using Microsoft.Extensions.Configuration;

namespace ApiEventConsumer
{
    public static class Program
    {

        public static async Task Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var appSettings = services.AddSettings(hostContext.Configuration);

                    services.AddBasicTinyHealthCheck(config =>
                    {
                        config.Port = appSettings.Port;
                        config.UrlPath = "/";
                        config.Hostname = "*";

                        return config;
                    });

                    if (appSettings.DatabaseSettings != null)
                    {
                        services.AddSingleton<IMongoClient>(sp =>
                            new MongoClient(appSettings.DatabaseSettings.ConnectionString));
                        services.AddSingleton<IMongoDatabase>(sp =>
                            sp.GetRequiredService<IMongoClient>().GetDatabase(appSettings.DatabaseSettings.DatabaseName));
                    }
                    services.AddChatBotServices(appSettings);
                    services.AddSubscriberServices(hostContext.Configuration);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration);
                    var sentryDsn = hostingContext.Configuration.GetSection("Sentry:Dsn").Value;
                    if (!string.IsNullOrEmpty(sentryDsn))
                    {
                        logging.AddSentry(sentryDsn);
                    }
                }).UseConsoleLifetime();
    }
}