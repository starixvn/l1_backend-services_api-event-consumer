namespace ApiEventConsumer.Providers
{
    public static class ProviderMiddleware
    {
        public static void AddProviderServices(this IServiceCollection services, ProviderSettings providerServiceSettings)
        {
            services.AddSingleton(providerServiceSettings);
            services.AddSingleton<IDapperContext, DapperContext>();
            services.AddScoped<IProviderService, ProviderService>();
        }
    }
}