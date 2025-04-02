using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiEventConsumer.Core.Constants;
using ApiEventConsumer.Core.Games;

namespace ApiEventConsumer.Core.Enumeration
{
    public class Game : Enumeration
    {
        public const string SeDieValue = "xocxoc";
        public const string DragonTigerValue = "dragontiger";
        public const string BigSmallValue = "taixiu";
        public const string BigSmallTurboValue = "txturbo";
        public const string FishPrawnCrabValue = "baucua";
        public const string OddEvenValue = "oddeven";
        public const string OddEvenTurboValue = "oeturbo";
        public const string KenoProMaxValue = "kenopromax";
        public const string BaccaratValue = "baccarat";
        public const string MiniSeDieValue = "minisedie";
        public const string KenoMaxValue = "kenomax";
        public const string KenoMax2Value = "kenomax2";
        public const string KenoMiniValue = "kenomini";
        public const string KenoMini2Value = "kenomini2";
        public const string KenoEastValue = "kenoeast";
        public const string KenoWestValue = "kenowest";
        public const string KenoSouthValue = "kenosouth";
        public const string KenoNorthValue = "kenonorth";
        public const string RouletteValue = "roulette";
        public const string LuckyWheelValue = "luckywheel";
        public const string VipSeDieValue = "vipsedie";
        public const string SpecialSeDieValue = "specialsedie";
        public const string BlackJackValue = "blackjack";
        public const string VipFishPrawnCrabValue = "vipfishprawncrab";
        public const string SicBoValue = "sicbo";
        public const string VipBaccaratValue = "vipbaccarat";
        public const string VipSeDieWarValue = "vipsediewar";
        public const string FourGuardiansValue = "fourguardians";
        public const string LotteryValue = "lottery";
        public const string ThreeCardsValue = "threecards";
        public const string HeroesSlotValue = "heroesslot";
        public const string CasinoHoldemPokerValue = "casinoholdempoker";
        public const string BitcoinMasterValue = "bitcoinmaster";
        public const string KenoMatch1Value = "kenomatch1";
        public const string KenoMatch2Value = "kenomatch2";
        public const string KenoPower1Value = "kenopower1";
        public const string KenoPower2Value = "kenopower2";
        public const string HiLoValue = "hilo";
        public const string TexasHoldemPokerValue = "texasholdempoker";
        public const string NightClubValue = "nightclub";
        public const string TreasureIslandValue = "treasureisland";
        public const string BlockchainBaccaratValue = "blockchainbaccarat";
        public const string WayToGoalValue = "waytogoal";
        public const string MegaKenoValue = "megakeno";
        public const string MegaKeno2Value = "megakeno2";
        public const string MiniSeDie2Value = "minisedie2";
        public const string MiniSeDie3Value = "minisedie3";
        public const string MiniSeDie4Value = "minisedie4";
        public const string UnbalancedBigSmall1Value = "unbalancedbigsmall1";
        public const string UnbalancedBigSmall2Value = "unbalancedbigsmall2";
        public const string UnbalancedBigSmall3Value = "unbalancedbigsmall3";
        public const string UnbalancedBigSmall4Value = "unbalancedbigsmall4";
        public const string FruitSlotValue = "fruitslot";
        public const string FanTanValue = "fantan";
        public const string FortuneCatValue = "fortunecat";
        public const string WinThreeCardsValue = "winthreecards";
        public const string DragonTigerFightValue = "dragontigerfight";
        public const string PontoonValue = "pontoon";
        public const string ColorGameValue = "colorgame";
        public const string BandarQiuQiuValue = "bandarqiuqiu";
        public const string ChineseBlackjackValue = "chineseblackjack";
        public const string BacBall1Value = "bacball1";
        public const string BacBall2Value = "bacball2";
        public const string LuckyNext1Value = "luckynext1";
        public const string LuckyNext2Value = "luckynext2";
        public const string FootballDiceValue = "footballdice";
        public const string FootballSlotValue = "footballslot";
        public const string CoinTossValue = "cointoss";
        public const string SeBall1Value = "seball1";
        public const string Bac81Value = "bac81";
        public const string LuckyThreeValue = "luckythree";
        public const string SeDie3Value = "sedie3";
        public const string SicBallValue = "sicball";
        public const string KaraKrusValue = "karakrus";
        public const string FootballCoinTossValue = "footballcointoss";
        public const string Bac82Value = "bac82";
        public const string Bac83Value = "bac83";
        public const string Bac84Value = "bac84";
        public const string Bac85Value = "bac85";
        public const string Bac86Value = "bac86";
        public const string Bac87Value = "bac87";
        public const string Bac88Value = "bac88";
        public const string Bac89Value = "bac89";
        public const string Bac90Value = "bac90";
        public const string Bac91Value = "bac91";
        public const string Bac92Value = "bac92";
        public const string Bac97Value = "bac97";
        public const string Bac98Value = "bac98";
        public const string Bac100Value = "bac100";
        public const string Bac101Value = "bac101";
        public const string Bac102Value = "bac102";
        public const string SevenUpDownValue = "sevenupdown";
        public const string MarbleFootballValue = "marblefootball";

        public static readonly Game BigSmall = new Game(BigSmallValue, "Big Small", GameId.BigSmall);
        public static readonly Game BigSmallTurbo = new Game(BigSmallTurboValue, "Turbo Big Small", GameId.BigSmallTurbo);
        public static readonly Game FishPrawnCrab = new Game(FishPrawnCrabValue, "Fish Prawn Crab", GameId.FishPrawnCrab);
        public static readonly Game SeDie = new Game(SeDieValue, "Se Die", GameId.SeDie);
        public static readonly Game OddEven = new Game(OddEvenValue, "Odd Even", GameId.OddEven);
        public static readonly Game OddEvenTurbo = new Game(OddEvenTurboValue, "Turbo Odd Even", GameId.OddEvenTurbo);
        public static readonly Game DragonTiger = new Game(DragonTigerValue, "Dragon Tiger", GameId.DragonTiger);
        public static readonly Game KenoProMax = new Game(KenoProMaxValue, "Keno Pro Max", GameId.KenoProMax);
        public static readonly Game Baccarat = new Game(BaccaratValue, "Baccarat", GameId.Baccarat);
        public static readonly Game MiniSeDie = new Game(MiniSeDieValue, "Mini Se Die", GameId.MiniSeDie);
        public static readonly Game KenoMax = new Game(KenoMaxValue, "Keno Max", GameId.KenoMax);
        public static readonly Game KenoMax2 = new Game(KenoMax2Value, "Keno Max2", GameId.KenoMax2);
        public static readonly Game KenoMini = new Game(KenoMiniValue, "Keno Mini", GameId.KenoMini);
        public static readonly Game KenoMini2 = new Game(KenoMini2Value, "Keno Mini 2", GameId.KenoMini2);
        public static readonly Game KenoEast = new Game(KenoEastValue, "Keno East", GameId.KenoEast);
        public static readonly Game KenoWest = new Game(KenoWestValue, "Keno West", GameId.KenoWest);
        public static readonly Game KenoSouth = new Game(KenoSouthValue, "Keno South", GameId.KenoSouth);
        public static readonly Game KenoNorth = new Game(KenoNorthValue, "Keno North", GameId.KenoNorth);
        public static readonly Game Roulette = new Game(RouletteValue, "Roulette", GameId.Roulette);
        public static readonly Game LuckyWheel = new Game(LuckyWheelValue, "Lucky Wheel");
        public static readonly Game VipSeDie = new Game(VipSeDieValue, "Vip Se Die", GameId.VipSeDie);
        public static readonly Game SpecialSeDie = new Game(SpecialSeDieValue, "Special SeDie", GameId.SpecialSeDie);
        public static readonly Game BlackJack = new Game(BlackJackValue, "Blackjack", GameId.Blackjack);
        public static readonly Game VipFishPrawnCrab = new Game(VipFishPrawnCrabValue, "Vip Fish Prawn Crab", GameId.VipFishPrawnCrab);
        public static readonly Game SicBo = new Game(SicBoValue, "Sic Bo", GameId.SicBo);
        public static readonly Game VipBaccarat = new Game(VipBaccaratValue, "Vip Baccarat", GameId.VipBaccarat);
        public static readonly Game VipSeDieWar = new Game(VipSeDieWarValue, "Se Die War", GameId.VipSeDieWar);
        public static readonly Game FourGuardians = new Game(FourGuardiansValue, "Four Guardians", GameId.FourGuardians);
        public static readonly Game Lottery = new Game(LotteryValue, "RNG Lottery", GameId.Lottery);
        public static readonly Game ThreeCards = new Game(ThreeCardsValue, "Three Cards", GameId.ThreeCards);
        public static readonly Game HeroesSlot = new Game(HeroesSlotValue, "Heroes Slot", GameId.HeroesSlot);
        public static readonly Game CasinoHoldemPoker = new Game(CasinoHoldemPokerValue, "Casino Hold'em", GameId.CasinoHoldemPoker);
        public static readonly Game BitcoinMaster = new Game(BitcoinMasterValue, "Bitcoin Master", GameId.BitcoinMaster);
        public static readonly Game KenoMatch1 = new Game(KenoMatch1Value, "Keno Soccer", GameId.KenoMatch1);
        public static readonly Game KenoMatch2 = new Game(KenoMatch2Value, "Keno Soccer 2", GameId.KenoMatch2);
        public static readonly Game KenoPower1 = new Game(KenoPower1Value, "Keno Power", GameId.KenoPower1);
        public static readonly Game KenoPower2 = new Game(KenoPower2Value, "Keno Power 2", GameId.KenoPower2);
        public static readonly Game HiLo = new Game(HiLoValue, "Hi-Lo", GameId.HiLo);
        public static readonly Game TexasHoldemPoker = new Game(TexasHoldemPokerValue, "Texas Hold'em", GameId.TexasHoldemPoker);
        public static readonly Game NightClub = new Game(NightClubValue, "Night Club", GameId.NightClub);
        public static readonly Game TreasureIsland = new Game(TreasureIslandValue, "Treasure Island", GameId.TreasureIsland);
        public static readonly Game WayToGoal = new Game(WayToGoalValue, "Way To Goal", GameId.WayToGoal);
        public static readonly Game BlockchainBaccarat = new Game(BlockchainBaccaratValue, "Blockchain Baccarat", GameId.BlockchainBaccarat);
        public static readonly Game MegaKeno = new Game(MegaKenoValue, "Mega Keno", GameId.MegaKeno);
        public static readonly Game UnbalancedBigSmall1 = new Game(UnbalancedBigSmall1Value, "Las Vegas", GameId.UnbalancedBigSmall1);
        public static readonly Game MegaKeno2 = new Game(MegaKeno2Value, "Mega Keno 2", GameId.MegaKeno2);
        public static readonly Game MiniSeDie2 = new Game(MiniSeDie2Value, "Mini Sedie 2", GameId.MiniSeDie2);
        public static readonly Game MiniSeDie3 = new Game(MiniSeDie3Value, "Mini Sedie 3", GameId.MiniSeDie3);
        public static readonly Game MiniSeDie4 = new Game(MiniSeDie4Value, "Mini Sedie 4", GameId.MiniSeDie4);
        public static readonly Game UnbalancedBigSmall2 = new Game(UnbalancedBigSmall2Value, "Marina Bay", GameId.UnbalancedBigSmall2);
        public static readonly Game UnbalancedBigSmall3 = new Game(UnbalancedBigSmall3Value, "Macau", GameId.UnbalancedBigSmall3);
        public static readonly Game UnbalancedBigSmall4 = new Game(UnbalancedBigSmall4Value, "Monte Carlo", GameId.UnbalancedBigSmall4);
        public static readonly Game FruitSlot = new Game(FruitSlotValue, "Fruit Slot", GameId.FruitSlot);
        public static readonly Game FanTan = new Game(FanTanValue, "Fan Tan", GameId.FanTan);
        public static readonly Game FortuneCat = new Game(FortuneCatValue, "Fortune Cat", GameId.FortuneCat);
        public static readonly Game WinThreeCards = new Game(WinThreeCardsValue, "Win Three Cards", GameId.WinThreeCards);
        public static readonly Game DragonTigerFight = new Game(DragonTigerFightValue, "Dragon Tiger Fight", GameId.DragonTigerFight);
        public static readonly Game Pontoon = new Game(PontoonValue, "Pontoon", GameId.Pontoon);
        public static readonly Game ColorGame = new Game(ColorGameValue, "Color Game", GameId.ColorGame);
        public static readonly Game BandarQiuQiu = new Game(BandarQiuQiuValue, "Bandar Qiu Qiu", GameId.BandarQiuQiu);
        public static readonly Game ChineseBlackjack = new Game(ChineseBlackjackValue, "Chinese Blackjack", GameId.ChineseBlackjack);
        public static readonly Game FootballDice = new Game(FootballDiceValue, "Football Dice", GameId.FootballDice);
        public static readonly Game FootballSlot = new Game(FootballSlotValue, "Football Slot", GameId.FootballSlot);
        public static readonly Game Bac81 = new Game(Bac81Value, "Lucky Baccarat", GameId.Bac81);
        public static readonly Game Bac82 = new Game(Bac82Value, "Lucky Baccarat 2", GameId.Bac82);
        public static readonly Game Bac83 = new Game(Bac83Value, "Lucky Baccarat 3", GameId.Bac83);
        public static readonly Game Bac84 = new Game(Bac84Value, "Baccarat 2", GameId.Bac84);
        public static readonly Game Bac85 = new Game(Bac85Value, "Baccarat 3", GameId.Bac85);
        public static readonly Game Bac86 = new Game(Bac86Value, "Baccarat 4", GameId.Bac86);
        public static readonly Game Bac87 = new Game(Bac87Value, "Baccarat 5", GameId.Bac87);
        public static readonly Game Bac88 = new Game(Bac88Value, "Lucky Baccarat 4", GameId.Bac88);
        public static readonly Game Bac89 = new Game(Bac89Value, "Lucky Baccarat 5", GameId.Bac89);
        public static readonly Game Bac90 = new Game(Bac90Value, "Baccarat 6", GameId.Bac90);
        public static readonly Game Bac91 = new Game(Bac91Value, "Baccarat 7", GameId.Bac91);
        public static readonly Game Bac92 = new Game(Bac92Value, "Baccarat 8", GameId.Bac92);
        public static readonly Game Bac97 = new Game(Bac97Value, "Baccarat 9", GameId.Bac97);
        public static readonly Game Bac98 = new Game(Bac98Value, "Speed Baccarat", GameId.Bac98);
        public static readonly Game Bac100 = new Game(Bac100Value, "Speed Baccarat 2", GameId.Bac100);
        public static readonly Game Bac101 = new Game(Bac101Value, "Speed Baccarat 3", GameId.Bac101);
        public static readonly Game Bac102 = new Game(Bac102Value, "Baccarat Lobby", GameId.Bac102);
        public static readonly Game SevenUpDown = new Game(SevenUpDownValue, "7 Up 7 Down", GameId.SevenUpDown);
        public Game()
        {
        }

        public Game(string value, string displayName)
            : base(value, displayName)
        {
        }

        public Game(string value, string displayName, GameId id)
            : base(value, displayName)
        {
            GameId = id;
        }

        public GameId GameId { get; }

        public static Game FromGameId(GameId id)
        {
            var gameSettings = GamesSettings.GetGamesSettings().GetGameFromId((int)id);
            if (gameSettings?.GameId > 0)
            {
                GameId a = (GameId)gameSettings.GameId;
                return new Game(gameSettings.GameName, gameSettings.DisplayName, a);
            }
            return Parse<Game, GameId>(id, "game id", item => item.GameId == id);
        }

        public static Game FromGameName(string gameName)
        {
            var gameSettings = GamesSettings.GetGamesSettings().GetGameFromName(gameName);
            if (gameSettings?.GameId > 0)
            {
                GameId a = (GameId)gameSettings.GameId;
                return new Game(gameSettings.GameName, gameSettings.DisplayName, a);
            }
            return Parse<Game, string>(gameName, "game value", item => item.Value == gameName);
        }

        public static bool ContainsGame(string gameName)
        {
            var gameSettings = GamesSettings.GetGamesSettings().GetGameFromName(gameName);
            if (gameSettings?.GameId > 0)
            {
                return true;
            }
            return Contains<Game>(gameName);
        }

        public static IEnumerable<Game> GetAllGamesBySystem(string systemType)
        {
            var gameSettings = GamesSettings.GetGamesSettings().Games
                                .Where(game => game.OfferedSystem.Contains(systemType))
                                .Select(game => new Game(game.GameName, game.DisplayName, (GameId)game.GameId));
            if (systemType.Equals(SystemType.SC1) || systemType.Equals(SystemType.VG2))
            {
                return GetAll<Game>().Concat(gameSettings);
            }
            else
            {
                return gameSettings;
            }
        }

        public static IEnumerable<Game> GetNormalGames()
        {
            var normalGame = new List<Game>
            {
                SeDie,
                FishPrawnCrab,
                DragonTiger,
                KenoProMax,
                MiniSeDie,
                Baccarat,
                KenoMax,
                KenoMax2,
                KenoMini,
                KenoMini2,
                KenoEast,
                KenoWest,
                KenoSouth,
                KenoNorth,
                Roulette,
                VipSeDie,
                SicBo,
                VipFishPrawnCrab,
                VipBaccarat,
                VipSeDieWar,
                Lottery,
                ThreeCards,
                KenoMatch1,
                KenoMatch2,
                KenoPower1,
                KenoPower2,
                BlockchainBaccarat,
                MegaKeno,
                MegaKeno2,
                MiniSeDie2,
                MiniSeDie3,
                MiniSeDie4,
                UnbalancedBigSmall1,
                UnbalancedBigSmall2,
                UnbalancedBigSmall3,
                UnbalancedBigSmall4,
                FruitSlot,
                FanTan,
                WinThreeCards,
                DragonTigerFight,
                ColorGame,
                BandarQiuQiu,
                FootballDice,
                FootballSlot,
                Bac81,
                Bac82,
                Bac83,
                Bac84,
                Bac85,
                Bac86,
                Bac87,
                Bac88,
                Bac89,
                Bac90,
                Bac91,
                Bac92,
                Bac97,
                Bac98,
                Bac100,
                Bac101,
                SevenUpDown,
            };

            var gameSettings = GamesSettings.GetGamesSettings().Games.Select(game => new Game(game.GameName, game.DisplayName, (GameId)game.GameId));
            return normalGame.Concat(gameSettings);
        }

        public static IEnumerable<Game> BaccaratXGames => new Game[]
        {
            Bac81, Bac82, Bac83, Bac84, Bac85, Bac86, Bac87, Bac88, Bac89, Bac90, Bac91, Bac92, Bac97, Bac98, Bac100, Bac101
        };
    }
}
