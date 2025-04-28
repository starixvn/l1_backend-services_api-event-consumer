using ApiEventConsumer._Shared.Middlewares;
using TinyHealthCheck;
using MongoDB.Driver;
using ApiEventConsumer.Service.Subscribers;
using Sentry.OpenTelemetry;
using Microsoft.Extensions.Configuration;

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
                    var sentrySetting = hostContext.Configuration.GetSection("Sentry");

                    SentrySdk.Init(options =>
                    {
                        options.Dsn = sentrySetting.GetValue<string>("Dsn");
                        options.SendDefaultPii = sentrySetting.GetValue<bool>("SendDefaultPii");
                        options.TracesSampleRate = 1.0;
                        options.UseOpenTelemetry();
                    });

                    services.AddSingleton<ISentryClient>(sp =>
                    {
                        return new SentryClient(new SentryOptions
                        {
                            Dsn = sentrySetting.GetValue<string>("Dsn"),
                            Environment = hostContext.Configuration["ENV"]
                        });
                    });

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
                    logging.AddSentry(opt => opt.Environment = hostingContext.Configuration["ENV"]);
                }).UseConsoleLifetime();
    }
}