using ApiEventConsumer._Shared.Middlewares;
using TinyHealthCheck;
using ApiEventConsumer.Subscribers;
using MongoDB.Driver;
using Sentry;

namespace ApiEventConsumer
{
    public static class Program
    {

        public static async Task Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            SentrySdk.CaptureMessage($"{DateTimeOffset.UtcNow} - Start ApiEventConsumer");

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

                    services.AddSchedulers();
                    services.AddEventListeners();
                    services.AddChatBotServices(appSettings);

                    if (appSettings.SubcriberSettings != null)
                    {
                        services.AddSubcriberServices(appSettings.SubcriberSettings);
                    }

                    if (appSettings.DatabaseSettings != null)
                    {
                        services.AddSingleton<IMongoClient>(sp =>
                            new MongoClient(appSettings.DatabaseSettings.ConnectionString));
                        services.AddSingleton<IMongoDatabase>(sp =>
                            sp.GetRequiredService<IMongoClient>().GetDatabase(appSettings.DatabaseSettings.DatabaseName));
                    }
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration);
                    logging.AddSentry(opt => opt.Environment = hostingContext.Configuration["ENV"]);
                }).UseConsoleLifetime();
    }
}