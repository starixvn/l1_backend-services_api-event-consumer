namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class SlotsConfig
    {
        public SlotsConfig() { }

        public SlotsConfig(byte mode, double bigWinRatio, Bonus bonus)
        {
            Mode = mode;
            BigWinRatio = bigWinRatio;
            Bonus = bonus;
        }

        public SlotsConfig(SlotsConfig config)
        {
            Mode = config.Mode;
            BigWinRatio = config.BigWinRatio;
            Bonus = config.Bonus;
        }

        public byte Mode { get; set; }
        public double BigWinRatio { get; set; }
        public Bonus Bonus { get; set; }
        public double[] BetLevels { get; set; }
        public List<StakeLevel> StakeLevels { get; set; }
        public Dictionary<string, Dictionary<string, double>> Icons { get; set; }
        public Jackpot Jackpot { get; set; }
        public Dictionary<string, int[][]> SymbolWeight { get; set; }
    }

    public class Bonus
    {
        public Bonus(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double Min { get; set; }

        public double Max { get; set; }
    }

    public class Jackpot
    {
        public Jackpot()
        {

        }

        public Jackpot(
            bool enabled,
            double winingAmount,
            JackpotPools pools,
            double playerContribution,
            double botContribution,
            bool excludeTestCurrency = false,
            string allowedCurrency = "USD"
            )
        {
            Enabled = enabled;
            WinningRatio = winingAmount;
            ExcludeTestCurrency = excludeTestCurrency;
            AllowedCurrency = (allowedCurrency ?? string.Empty).Trim();
            PlayerContributionRatio = playerContribution;
            BotContributionRatio = botContribution;
            Pools = pools;
        }

        public bool Enabled { get; set; }

        public bool ExcludeTestCurrency { get; set; }

        public double WinningRatio { get; set; }

        public double PlayerContributionRatio { get; set; }

        public double BotContributionRatio { get; set; }

        public JackpotPools Pools { get; set; }

        public string AllowedCurrency { get; set; }
    }

    public class JackpotPools
    {
        public Pool Minor { get; set; }
        public Pool Major { get; set; }
        public Pool Grand { get; set; }
    }

    public class Pool
    {
        public double Init { get; set; }
        public UserPoolSettings PlayerSettings { get; set; }
        public UserPoolSettings BotSettings { get; set; }
    }

    public class UserPoolSettings
    {
        public double Contrib { get; set; }
        public double Alloc { get; set; }
    }

}