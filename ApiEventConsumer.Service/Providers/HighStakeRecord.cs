namespace ApiEventConsumer.Providers
{
    public record HighStakeRecord
    {
        public DateTime HighStakeDate { get; init; }

        public string Code { get; init; }

        public string BaseCode { get; init; }

        public decimal BaseRate { get; init; }
    }
}