using Coravel;

namespace ApiEventConsumer._Shared.Middlewares
{
    public static class ServiceMiddleware
    {
        public static void AddSchedulers(this IServiceCollection services)
        {
            services.AddScheduler();
        }

        public static void AddEventListeners(this IServiceCollection services)
        {
            services.AddEvents();
        }
    }
}
