using ApiEventConsumer._Shared.Middlewares;
using TinyHealthCheck;
using MongoDB.Driver;
using ApiEventConsumer.Service.Subscribers;
using Microsoft.Extensions.Configuration;
using Sentry;

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

                    // Register ISentryClient for dependency injection
                    services.AddSingleton<ISentryClient>(sp =>
                    {
                        var sentrySection = hostContext.Configuration.GetSection("Sentry");
                        return new SentryClient(new SentryOptions
                        {
                            Dsn = sentrySection.GetValue<string>("Dsn"),
                            Environment = sentrySection.GetValue<string>("Environment"),
                            SendDefaultPii = sentrySection.GetValue<bool>("SendDefaultPii"),
                            AttachStacktrace = sentrySection.GetValue<bool>("AttachStacktrace"),
                            Debug = sentrySection.GetValue<bool>("Debug"),
                            TracesSampleRate = sentrySection.GetValue<double>("TracesSampleRate")
                        });
                    });

                    services.AddBasicTinyHealthCheck(config =>
                    {
                        config.Port = appSettings.Port;
                        config.UrlPath = "/";
                        config.Hostname = "*";

                        return config;
                    });

                    services.AddSingleton<IMongoClient>(sp =>
                        new MongoClient(appSettings.DatabaseSettings.ConnectionString));
                    services.AddSingleton<IMongoDatabase>(sp =>
                        sp.GetRequiredService<IMongoClient>().GetDatabase(appSettings.DatabaseSettings.DatabaseName));
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