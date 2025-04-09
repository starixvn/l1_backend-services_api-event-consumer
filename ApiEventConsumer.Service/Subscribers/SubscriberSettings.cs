using NATS.Client.JetStream;

namespace ApiEventConsumer.Subscribers
{
    public class SubscriberSettings
    {
        public SubcriberDatabase? SC { get; set; }

        public SubcriberDatabase? VG { get; set; }

        public SubcriberDatabase? GN { get; set; }
    }

    public class SubcriberDatabase
    {
        public string? SystemType { get; set; }
        public StreamConfig? StreamConfig { get; set; }

    }

    public class StreamConfig
    {
        public string? StreamName { get; set; }
        public string? BetMessages { get; set; }
        public string? SettledMessages { get; set; }
        public bool? Enable { get; set; }

    }
}