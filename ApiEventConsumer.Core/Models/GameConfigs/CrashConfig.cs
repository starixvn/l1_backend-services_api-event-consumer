using System.Collections.Generic;

namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class CrashConfig
    {
        public int MaxMultiplier { get; set; } = 0;

        public int MaxWinPerRound { get; set; } = 0;

        public double CrashAtBase { get; set; } = 0;

        public MultiplierConfig? MultiplierConfigs { get; set; }
    }

    public class MultiplierConfig
    {
        public List<MultiplierStage>? Stages { get; set; }

        public double InitialMultiplier { get; set; }
    }

    public class MultiplierStage
    {
        public double MinMultiplier { get; set; }
        public double MaxMultiplier { get; set; }
        public double Increment { get; set; }
    }
}
