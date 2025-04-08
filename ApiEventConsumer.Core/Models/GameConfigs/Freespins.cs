namespace ApiEventConsumer.Core.Models.GameConfigs
{
    public class FreeSpins
    {
        public FreeSpins()
        {
        }

        public FreeSpins(List<FreeSpinsOdds> initialOdds, List<FreeSpinsOdds> additionalOdds)
        {
            InitialOdds = initialOdds;
            AdditionalOdds = additionalOdds;
        }

        public List<FreeSpinsOdds>? InitialOdds { get; set; }
        public List<FreeSpinsOdds>? AdditionalOdds { get; set; }
    }
}
