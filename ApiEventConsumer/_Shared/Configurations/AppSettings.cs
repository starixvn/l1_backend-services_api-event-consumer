namespace ApiEventConsumer._Shared.Configurations
{
    public partial interface IAppSettings
    {
        int Port { get; set; }

        string Cron { get; set; }
    }

    public partial class AppSettings : IAppSettings
    {
        public int Port { get; set; }

        public string Cron { get; set; }
    }
}
