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
    public class BetSettledSubscriber(
        ISentryClient sentryClient,
        IAppSettings appSettings,
        IPromotionService promotionService,
        INotificationService notificationService) : IEventSubscriber
    {
        private const decimal HighMultiplierThreshold = 3.0m;

        private readonly ISentryClient sentryClient = sentryClient;
        private readonly IAppSettings appSettings = appSettings;
        private readonly IPromotionService promotionService = promotionService;
        private readonly INotificationService notificationService = notificationService;

        public string SubjectName => appSettings?.StreamSettings?.BetSettledMessages ?? string.Empty;

        public async Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betSettled = JsonSerializer.Deserialize<BetSettled>(message);

                if (betSettled != null)
                {
                    await ProcessBetSettled(betSettled);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return false;
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
                    BetAmount = betSettled.ValidBetAmount
                };

                var response = await promotionService.UpdateTotalTurnoverAsync(request);
                
                // Send notification if promotion service was successful
                if (response.Success)
                {
                    await NotifyProgress(betSettled, response);
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private void HandleNormalTicket(BetSettled betSettled)
        {
            try
            {
                if (betSettled.TotalAmount <= 0 || betSettled.TotalWin < betSettled.TotalAmount * HighMultiplierThreshold)
                {
                    return;
                }

                var multiplier = betSettled.TotalWin / betSettled.TotalAmount;

                if (multiplier > HighMultiplierThreshold)
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
            }
        }

        private async Task NotifyProgress(BetSettled betSettled, UpdateTotalTurnoverResponse response)
        {
            try
            {
                // Validate required fields
                var playerId = betSettled.PlayerId?.ToString() ?? string.Empty;
                var operatorId = betSettled.OperatorId ?? string.Empty;
                var siteName = betSettled.SiteName ?? string.Empty;

                if (string.IsNullOrWhiteSpace(playerId) || 
                    string.IsNullOrWhiteSpace(operatorId) || 
                    string.IsNullOrWhiteSpace(siteName))
                {
                    sentryClient.CaptureMessage($"Missing required fields for notification: PlayerId={playerId}, OperatorId={operatorId}, SiteName={siteName}");
                    return;
                }

                var notificationRequest = new PromotionProgressRequest
                {
                    PlayerId = playerId,
                    OperatorId = operatorId,
                    SiteName = siteName,
                    TotalTO = response.TotalTurnover,
                    MinTO = 0, // Not available in response
                    RemainTO = response.RemainingTO,
                    Progress = 0, // Not available in response
                    CanClaim = false, // Not available in response
                    Win = betSettled.TotalWin,
                    Expire = DateTime.MinValue // Not available in response
                };

                var notificationResponse = await notificationService.SendPromotionProgressAsync(notificationRequest);
                
                if (!notificationResponse.Success)
                {
                    sentryClient.CaptureMessage($"Notification failed: {notificationResponse.Code} - {notificationResponse.Message}");
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }
    }
}