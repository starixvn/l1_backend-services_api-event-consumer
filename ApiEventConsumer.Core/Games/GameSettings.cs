using Newtonsoft.Json;

namespace ApiEventConsumer.Core.Games
{
    public class GameSettings
    {
        public int GameId { get; set; }
        public string? GameName { get; set; }
        public string? DisplayName { get; set; }
        public IEnumerable<string>? BetChoices { get; set; }
        public IEnumerable<string>? Odds { get; set; }
        public IEnumerable<string>? OfferedSystem { get; set; }
    }

    public class GamesSettings
    {
        private static GamesSettings? _instance;

        public static GamesSettings GetGamesSettings()
        {
            return _instance ??= new GamesSettings();
        }

        public IEnumerable<GameSettings>? Games { get; }

        public GamesSettings()
        {
        }

        public GameSettings? GetGameFromName(string gameName)
        {
            return Games?.FirstOrDefault(game => game.GameName == gameName);
        }

        public GameSettings? GetGameFromId(int gameId)
        {
            return Games?.FirstOrDefault(game => game.GameId == gameId);
        }
    }
}