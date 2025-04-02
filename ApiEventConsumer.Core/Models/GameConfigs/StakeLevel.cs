namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class StakeLevel
    {
        public StakeLevel() { }

        public StakeLevel(double stake, double jackpot, double? botRatio) 
        {
            Stake = stake;
            Jackpot = jackpot;
            BotRatio = botRatio;
        }

        public double Stake { get; set; }

        public double Jackpot { get; set; }

        public double? BotRatio { get; set; }
    }
}
