namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class MultiplierBonus
    {
        public MultiplierBonus() { }

        public MultiplierBonus(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double Min { get; set; }

        public double Max { get; set; }
    }
}
