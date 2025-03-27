using ApiEventConsumer.Subscribers;

namespace ApiEventConsumer._Shared.Configurations
{
    public partial interface IAppSettings
    {
        SubscriberSettings SubcriberSettings { get; set; }
    }

    public partial class AppSettings : IAppSettings
    {
        public SubscriberSettings SubcriberSettings { get; set; }
    }
}