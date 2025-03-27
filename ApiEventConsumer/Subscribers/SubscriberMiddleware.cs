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

            services.AddScoped<ISCHighStakeDataAccess, SCHighStakeDataAccess>();
            services.AddScoped<ISCGameConfigDataAccess, SCGameConfigDataAccess>();
            services.AddScoped<ISCSubscriberService, SCSubscriberService>();

            services.AddScoped<IVGHighStakeDataAccess, VGHighStakeDataAccess>();
            services.AddScoped<IVGGameConfigDataAccess, VGGameConfigDataAccess>();
            services.AddScoped<IVGSubscriberService, VGSubcriberService>();

            services.AddScoped<ISNHighStakeDataAccess, SNHighStakeDataAccess>();
            services.AddScoped<ISNGameConfigDataAccess, SNGameConfigDataAccess>();
            services.AddScoped<ISNSubscriberService, SNSubscriberService>();
        }

        public static void AddMongoDBServices(this IServiceCollection services, SubscriberSettings settings)
        {
            var camelIgnoringExtraElementConventions = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };

            ConventionRegistry.Register(nameof(camelIgnoringExtraElementConventions), camelIgnoringExtraElementConventions, t => true);
            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));

            services.AddSingleton<ISCMongoClient>(new SCMongoClient(settings.SC));
            services.AddSingleton<IVGMongoClient>(new VGMongoClient(settings.VG));
            services.AddSingleton<ISNMongoClient>(new SNMongoClient(settings.SN));
        }
    }
}