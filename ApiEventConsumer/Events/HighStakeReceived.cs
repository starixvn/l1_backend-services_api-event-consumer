using Coravel.Events.Interfaces;

namespace ApiEventConsumer.Events
{
    public class HighStakeReceived(string source, IEnumerable<Providers.HighStakeRecord> highStake) : IEvent
    {
        public string Source { get; init; } = source;

        public IEnumerable<Providers.HighStakeRecord> HighStake { get; init; } = highStake;
    }
}
