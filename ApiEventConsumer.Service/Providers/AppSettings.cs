using ApiEventConsumer.Providers;

namespace ApiEventConsumer._Shared.Configurations
{
    public partial interface IAppSettings
    {
        ProviderSettings ProviderSettings { get; set; }
    }

    public partial class AppSettings : IAppSettings
    {
        public ProviderSettings ProviderSettings { get; set; }
    }
}