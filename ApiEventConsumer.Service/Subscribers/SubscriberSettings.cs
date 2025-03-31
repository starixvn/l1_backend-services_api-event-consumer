namespace ApiEventConsumer.Subscribers
{
    public class SubscriberSettings
    {
        public SubcriberDatabase SC { get; set; }

        public SubcriberDatabase VG { get; set; }

        public SubcriberDatabase SN { get; set; }
    }

    public class SubcriberDatabase
    {
        public string ConnectionString { get; set; }

        public string DatabaseName { get; set; }

        public bool Enabled { get; set; }
    }
}