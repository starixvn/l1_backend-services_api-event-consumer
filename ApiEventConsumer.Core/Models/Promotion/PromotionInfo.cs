namespace ApiEventConsumer.Core.Models.Promotion
{
    public class BetLevel
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class CoinValue
    {
        public decimal Value { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class Promotion
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public List<string> OfferedSiteNames { get; set; } = [];
        public List<string> CurrencyGroups { get; set; } = [];
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime ClaimExpirationDate { get; set; }
        public int BonusItemCount { get; set; }
        public List<string> OfferedPlayerIds { get; set; } = [];
        public int GameConfigType { get; set; }
        public List<int> GameIds { get; set; } = [];
        public List<BetLevel> BetLevels { get; set; } = [];
        public List<CoinValue> CoinValues { get; set; } = [];
        public bool IsGiftCode { get; set; }
        public decimal MinTurnoverMultiplier { get; set; } = 2.0m;
    }
}
