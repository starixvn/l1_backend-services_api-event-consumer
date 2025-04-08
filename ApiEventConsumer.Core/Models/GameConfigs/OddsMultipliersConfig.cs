using System.Collections.Generic;
using System.ComponentModel;

namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class OddsMultipliersConfig
    {
        private const int PERCENTAGE_MULTIPLIER = 100;

        public double ExtraFeeRatio { get; set; }

        [DisplayName("Extra Fee (%)")]
        public double ExtraFeePercentage
        {
            get
            {
                return ExtraFeeRatio * PERCENTAGE_MULTIPLIER;
            }
            set
            {
                ExtraFeeRatio = value / PERCENTAGE_MULTIPLIER;
            }
        }

        public NumberOfCardsRange NumberOfCardsRange { get; set; } = new NumberOfCardsRange();
        public List<MultiplierDistribution>? Values { get; set; }
    }

    public class NumberOfCardsRange
    {
        [DisplayName("From")]
        public int Min { get; set; }

        [DisplayName("To")]
        public int Max { get; set; }
    }

    public class MultiplierDistribution
    {
        public double Number { get; set; }
        public double Ratio { get; set; }
    }
}