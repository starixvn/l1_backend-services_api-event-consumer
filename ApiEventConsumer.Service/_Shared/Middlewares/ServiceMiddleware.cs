using ApiEventConsumer.Providers;
using ApiEventConsumer.Subscribers;
using Coravel;

namespace ApiEventConsumer._Shared.Middlewares
{
    public static class ServiceMiddleware
    {
        public static void AddSchedulers(this IServiceCollection services)
        {
            services.AddScheduler();

            services.AddTransient<HighStakeInvocable>();
        }

        public static void AddEventListeners(this IServiceCollection services)
        {
            services.AddEvents();

            services.AddTransient<HighStakeListener>();
        }
    }
}
