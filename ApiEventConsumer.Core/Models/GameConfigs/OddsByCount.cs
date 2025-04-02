namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class OddsByCount
    {
        public OddsByCount() { }

        public OddsByCount(int from, int to, double odds)
        {
            From = from;
            To = to;
            Odds = odds;
        }

        public int From { get; set; }

        public int To { get; set; }

        public double Odds { get; set; }
    }
}
