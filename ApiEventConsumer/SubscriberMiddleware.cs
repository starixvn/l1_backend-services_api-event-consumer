using ApiEventConsumer.Database;
using Microsoft.Extensions.DependencyInjection;

namespace ApiEventConsumer
{
    public static class SubscriberMiddleware
    {
        public static void AddSubscriberServices(this IServiceCollection services)
        {
            // Register repositories
            services.AddSingleton(typeof(IGenericRepository<>), typeof(MongoRepository<>));
            
            // Register other services
            services.AddSingleton<SubscriberService>();
            services.AddSingleton<NatsService>();
        }
    }
} 