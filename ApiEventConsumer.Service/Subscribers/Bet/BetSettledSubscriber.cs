using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Services;
using NATS.Client;

namespace ApiEventConsumer.Subscribers.Bet
{
    public class BetSettledSubscriber(
        ISentryClient sentryClient,
        IAppSettings appSettings,
        IPromotionService promotionService,
        INotificationService notificationService,
        IChatBotApi chatBotApi,
        IGeneralGameSettingService generalGameSettingService,
        ISharedCacheService sharedCache) : IEventSubscriber
    {
        private const decimal HighMultiplierThreshold = 10.0m;

        private readonly ISentryClient sentryClient = sentryClient;
        private readonly IAppSettings appSettings = appSettings;
        private readonly IPromotionService promotionService = promotionService;
        private readonly INotificationService notificationService = notificationService;
        private readonly IChatBotApi chatBotApi = chatBotApi;
        private readonly IGeneralGameSettingService generalGameSettingServeice = generalGameSettingService;
        private readonly ISharedCacheService sharedCache = sharedCache;
        public string SubjectName => appSettings?.StreamSettings?.BetSettledMessages ?? string.Empty;

        public async Task<bool> OnMessageReceived(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betSettled = JsonSerializer.Deserialize<BetSettled>(message);

                if (betSettled != null)
                {
                    await ProcessBetSettled(betSettled);
                }

                return true;
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
                await HandleHighMultiplier(betSettled);
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
                if (!await IsTicketEligibleForPromotion(betSettled))
                {
                    return;
                }

                var request = new UpdateTotalTurnoverRequest
                {
                    PlayerId = betSettled.PlayerId?.ToString() ?? string.Empty,
                    OperatorId = betSettled.OperatorId ?? string.Empty,
                    SiteName = betSettled.SiteName ?? string.Empty,
                    BetAmount = betSettled.ValidBetAmount,
                    GameId = betSettled.GameTypeId,
                    BaseBetAmount = betSettled.BaseTotalAmount
                };

                var response = await promotionService.UpdateTotalTurnoverAsync(request);

                if (string.IsNullOrEmpty(response.ErrorCode))
                {
                    await NotifyProgress(betSettled, response);
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private async Task<bool> IsTicketEligibleForPromotion(BetSettled betSettled)
        {
            try
            {
                var promotions = await GetActivePromotions();
                if (promotions.Count == 0)
                {
                    return false;
                }

                var currentTime = DateTime.UtcNow;
                return promotions.Any(promotion => IsPromotionEligibleForTicket(promotion, betSettled, currentTime));
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return false;
            }
        }

        private async Task<List<Promotion>> GetActivePromotions()
        {
            var cacheKey = PromotionCacheService.GetCacheKeyRunningPromotions();
            var cachedPromotions = sharedCache.Get<List<Promotion>>(cacheKey);

            if (cachedPromotions?.Count > 0)
            {
                return cachedPromotions;
            }

            var response = await promotionService.GetRunningPromotionsAsync();
            return response ?? [];
        }

        private static bool IsPromotionEligibleForTicket(Promotion promotion, BetSettled betSettled, DateTime currentTime)
        {
            var isTimeValid = promotion.StartDate <= currentTime && promotion.ClaimExpirationDate >= currentTime;
            var isTargetingValid = IsEligibleForSite(promotion, betSettled.SiteName) && IsEligibleForPlayer(promotion, betSettled.PlayerId);
            var isGameAndOperatorValid = promotion.GameIds.Contains(betSettled.GameTypeId) && promotion.OperatorId == betSettled.OperatorId;

            return isTimeValid && isTargetingValid && isGameAndOperatorValid;
        }

        private static bool IsEligibleForSite(Promotion promotion, string? siteName)
        {
            return promotion.OfferedSiteNames.Count == 0 
                || promotion.OfferedSiteNames.Contains(siteName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsEligibleForPlayer(Promotion promotion, object? playerId)
        {
            return promotion.OfferedPlayerIds.Count == 0 
                || promotion.OfferedPlayerIds.Contains(playerId?.ToString() ?? string.Empty);
        }

        private async Task HandleHighMultiplier(BetSettled betSettled)
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
                    // Get the game name using our cached service
                    var gameName = await generalGameSettingServeice.GetDisplayName(betSettled.GameTypeId);

                    var message = new StringBuilder("HIGH MULTIPLIER DETECTED:");
                    message.Append($" PlayerId={betSettled.PlayerId}");
                    message.Append($", GameTypeId={betSettled.GameTypeId}");
                    message.Append($", GameName={gameName}");
                    message.Append($", GameRoundId={betSettled.GameRoundId}");
                    message.Append($", TotalWin={betSettled.TotalWin}");
                    message.Append($", Multiplier={multiplier:F2}x");

                    await chatBotApi.ForwardMessage(
                                   appSettings.ChatBotApiSettings?.Token ?? string.Empty,
                                   appSettings.ChatBotApiSettings?.ConversationIds?.HighMultiplier ?? string.Empty,
                               message.ToString());
                    return;
                }
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
                    MinTO = response.MinRequiredTurnover,
                    RemainTO = response.RemainingTO,
                    Progress = response.ProgressPercentage,
                    CanClaim = response.CanClaim,
                    Win = response.TotalWinAmount,
                    Expire = response.ClaimExpirationDate
                };

                await notificationService.SendPromotionProgressAsync(notificationRequest);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }
    }
}