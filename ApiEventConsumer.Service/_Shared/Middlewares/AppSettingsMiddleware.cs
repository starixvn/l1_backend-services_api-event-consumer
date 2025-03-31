using ApiEventConsumer._Shared.Configurations;

namespace ApiEventConsumer._Shared.Middlewares
{
    public static class AppSettingsMiddleware
    {
        public static AppSettings AddSettings(this IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = new AppSettings();

            configuration.GetSection("AppSettings").Bind(appSettings);
            services.AddSingleton<IAppSettings>(appSettings);

            return appSettings;
        }
    }
}
