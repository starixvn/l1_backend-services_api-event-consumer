using ApiEventConsumer.Subscribers.SC;
using ApiEventConsumer.Subscribers.SN;
using ApiEventConsumer.Subscribers.VG;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace ApiEventConsumer.Subscribers
{
    public static class SubscriberMiddleware
    {
        public static void AddSubcriberServices(this IServiceCollection services, SubscriberSettings settings)
        {
            services.AddSingleton(settings);
            services.AddHostedService<SCSubscriberService>();
            services.AddHostedService<VGSubcriberService>();
            services.AddHostedService<SNSubscriberService>();
        }
    }
}