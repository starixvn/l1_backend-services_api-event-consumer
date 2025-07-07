using System;
using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Service.Service;
using NATS.Client;

namespace ApiEventConsumer.Subscribers.Bet
{
    public class BetSettledSubscriber : IEventSubscriber
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;

        public BetSettledSubscriber(
            ISentryClient sentryClient,
            ConfigurationServices configurations)
        {
            this.sentryClient = sentryClient;
            this.appSettings = configurations.AppSettings;
        }

        public string SubjectName => appSettings?.StreamSettings?.BetSettledMessages ?? string.Empty;

        public void OnMessageReceived(object sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betSettled = JsonSerializer.Deserialize<BetSettled>(message);
                var systemType = appSettings.SystemType ?? string.Empty;

                if (betSettled != null)
                {
                    // Log the bet settled event for now
                    // TODO: Add specific business logic for handling bet settled events
                    Console.WriteLine($"Bet settled received: GameRoundId={betSettled.GameRoundId}, " +
                                    $"GameTypeId={betSettled.GameTypeId}, TotalAmount={betSettled.TotalAmount}, " +
                                    $"TotalWin={betSettled.TotalWin}, ValidBetAmount={betSettled.ValidBetAmount}, " +
                                    $"FreeBet={betSettled.FreeBet}, PromotionId={betSettled.PromotionId}, " +
                                    $"PlayerId={betSettled.PlayerId}, SiteId={betSettled.SiteId}");
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }
    }
} 