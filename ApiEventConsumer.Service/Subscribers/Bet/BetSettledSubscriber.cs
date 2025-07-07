using System;
using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Service.Service;
using NATS.Client;

namespace ApiEventConsumer.Subscribers.Bet
{
    public class BetSettledSubscriber : IEventSubscriber
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private readonly IPromotionService promotionService;

        public BetSettledSubscriber(
            ISentryClient sentryClient,
            IAppSettings appSettings,
            IPromotionService promotionService)
        {
            this.sentryClient = sentryClient;
            this.appSettings = appSettings;
            this.promotionService = promotionService;
        }

        public string SubjectName => appSettings?.StreamSettings?.BetSettledMessages ?? string.Empty;

        public void OnMessageReceived(object sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betSettled = JsonSerializer.Deserialize<BetSettled>(message);

                if (betSettled != null)
                {
                    Task.Run(async () => {
                        await ProcessBetSettled(betSettled);
                    });
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private async Task ProcessBetSettled(BetSettled betSettled)
        {
            try
            {                
                await HandlePromotionalTicket(betSettled);                   

                HandleNormalTicket(betSettled);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private async Task HandlePromotionalTicket(BetSettled betSettled)
        {
            try
            {
                var request = new UpdateTotalTurnoverRequest
                {
                    PlayerId = betSettled.PlayerId?.ToString() ?? string.Empty,
                    OperatorId = betSettled.OperatorId ?? string.Empty,
                    SiteName = betSettled.SiteName ?? string.Empty,
                    PromotionId = betSettled.PromotionId,
                    BetAmount = betSettled.ValidBetAmount
                };

                var response = await promotionService.UpdateTotalTurnoverAsync(request);

                if (response.Success)
                {
                    Console.WriteLine($"Promotional ticket processed successfully: GameRoundId={betSettled.GameRoundId}, " +
                                    $"PromotionId={betSettled.PromotionId}, ValidBetAmount={betSettled.ValidBetAmount}");
                    return;
                }

                Console.WriteLine($"Failed to process promotional ticket: GameRoundId={betSettled.GameRoundId}, " +
                                $"PromotionId={betSettled.PromotionId}, Error={response.Message}");
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                Console.WriteLine($"Exception processing promotional ticket: GameRoundId={betSettled.GameRoundId}, " +
                                $"PromotionId={betSettled.PromotionId}, Exception={ex.Message}");
            }
        }

        private void HandleNormalTicket(BetSettled betSettled)
        {
            try
            {
                if (betSettled.TotalAmount <= 0)
                {
                    Console.WriteLine($"Normal ticket with zero amount: GameRoundId={betSettled.GameRoundId}, " +
                                    $"TotalAmount={betSettled.TotalAmount}, TotalWin={betSettled.TotalWin}");
                    return;
                }

                var multiplier = betSettled.TotalWin / betSettled.TotalAmount;

                if (multiplier > 3)
                {
                    Console.WriteLine($"HIGH MULTIPLIER DETECTED: GameRoundId={betSettled.GameRoundId}, " +
                                    $"GameTypeId={betSettled.GameTypeId}, TotalAmount={betSettled.TotalAmount}, " +
                                    $"TotalWin={betSettled.TotalWin}, Multiplier={multiplier:F2}x, " +
                                    $"PlayerId={betSettled.PlayerId}, SiteId={betSettled.SiteId}");
                    return;
                }

                Console.WriteLine($"Normal ticket processed: GameRoundId={betSettled.GameRoundId}, " +
                                $"Multiplier={multiplier:F2}x, TotalAmount={betSettled.TotalAmount}, " +
                                $"TotalWin={betSettled.TotalWin}");
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                Console.WriteLine($"Exception processing normal ticket: GameRoundId={betSettled.GameRoundId}, " +
                                $"Exception={ex.Message}");
            }
        }
    }
} 