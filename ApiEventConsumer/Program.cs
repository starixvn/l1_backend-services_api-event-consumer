using ApiEventConsumer._Shared.Middlewares;
using ApiEventConsumer.Providers;
using ApiEventConsumer.Subscribers;
using Coravel;
using Coravel.Events.Interfaces;
using ApiEventConsumer.Events;
using TinyHealthCheck;

namespace ApiEventConsumer
{
    public static class Program
    {
        private static string CronSettings = "";

        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            SentrySdk.CaptureMessage($"{DateTime.Now} - Start ApiEventConsumer", SentryLevel.Info);

            host.Services.UseScheduler(scheduler =>
            {
                scheduler.Schedule<Invocable>().Cron(CronSettings);
            }).OnError((exception) =>
                  SentrySdk.CaptureException(exception)
            );

            IEventRegistration registration = host.Services.ConfigureEvents();

            registration
                .Register<HighStakeReceived>()
                .Subscribe<HighStakeListener>();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var appSettings = services.AddSettings(hostContext.Configuration);
                    CronSettings = appSettings.Cron;
                    services.AddBasicTinyHealthCheck(config =>
                    {
                        config.Port = appSettings.Port;
                        config.UrlPath = "/";

                        return config;
                    });

                    services.AddSchedulers();
                    services.AddEventListeners();

                    services.AddMongoDBServices(appSettings.SubcriberSettings);

                    services.AddProviderServices(appSettings.ProviderSettings);
                    services.AddSubcriberServices(appSettings.SubcriberSettings);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration);
                    logging.AddSentry(opt => opt.Environment = hostingContext.Configuration["ENV"]);
                }).UseConsoleLifetime();
    }
}