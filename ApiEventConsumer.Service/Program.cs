using ApiEventConsumer._Shared.Middlewares;
using TinyHealthCheck;
using ApiEventConsumer.Subscribers;
using ApiEventConsumer.Subscribers.SC;
using ApiEventConsumer.Subscribers.VG;
using ApiEventConsumer.Subscribers.SN;
using MongoDB.Driver;
using static Dapper.SqlMapper;

namespace ApiEventConsumer
{
    public static class Program
    {

        public static async Task Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            SentrySdk.CaptureMessage($"{DateTime.Now} - Start ApiEventConsumer", SentryLevel.Info);

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

                        return config;
                    });

                    services.AddSchedulers();
                    services.AddEventListeners();
                    services.AddChatBotServices(appSettings);

                    services.AddSubcriberServices(appSettings.SubcriberSettings);

                    services.AddSingleton<IMongoClient>(sp =>
                        new MongoClient(appSettings.DatabaseSettings.ConnectionString));
                    services.AddSingleton<IMongoDatabase>(sp =>
                        sp.GetRequiredService<IMongoClient>().GetDatabase(appSettings.DatabaseSettings.DatabaseName));
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration);
                    logging.AddSentry(opt => opt.Environment = hostingContext.Configuration["ENV"]);
                }).UseConsoleLifetime();
    }
}