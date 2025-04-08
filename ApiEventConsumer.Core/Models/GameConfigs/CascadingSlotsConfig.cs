using System.Collections.Generic;

namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class CascadingSlotsConfig
    {
        public CascadingSlotsConfig() { }

        public CascadingSlotsConfig(byte mode, double bigWinRatio, MultiplierBonus multiplierBonus)
        {
            Mode = mode;
            BigWinRatio = bigWinRatio;
            MultiplierBonus = multiplierBonus;
        }

        public CascadingSlotsConfig(CascadingSlotsConfig config)
        {
            Mode = config.Mode;
            BigWinRatio = config.BigWinRatio;
            MultiplierBonus = config.MultiplierBonus;
            BetLevels = config.BetLevels;
            BetMultiplier = config.BetMultiplier;
            MaxWinPerRound = config.MaxWinPerRound;
            OddsByCounts = config.OddsByCounts;
        }

        public byte Mode { get; set; }
        public double BigWinRatio { get; set; }
        public MultiplierBonus? MultiplierBonus { get; set; }
        public int[]? BetLevels { get; set; }
        public double BetMultiplier { get; set; }
        public double MaxWinPerRound { get; set; }
        public Dictionary<string, List<OddsByCount>>? OddsByCounts { get; set; }
        public FreeSpins? FreeSpins { get; set; }
    }
}
