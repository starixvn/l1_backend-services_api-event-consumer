namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class FreeSpinsOdds
    {
        public FreeSpinsOdds()
        {
        }

        public FreeSpinsOdds(int count, double odds)
        {
            Count = count;
            Odds = odds;
        }

        public int Count { get; set; }
        public double Odds { get; set; }
    }
}
