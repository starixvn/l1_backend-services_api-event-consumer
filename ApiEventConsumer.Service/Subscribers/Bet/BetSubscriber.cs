using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Services;
using NATS.Client;

namespace ApiEventConsumer.Subscribers.Bet
{
    public class BetSubscriber(
        ISentryClient sentryClient,
        IAppSettings appSettings,
        IHighStakeServices highStakeServices) : IEventSubscriber
    {
        private readonly ISentryClient sentryClient = sentryClient;
        private readonly IAppSettings appSettings = appSettings;
        private readonly IHighStakeServices highStakeServices = highStakeServices;

        public string SubjectName => appSettings?.StreamSettings?.BetMessages ?? string.Empty;

        public async Task<bool> OnMessageReceived(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                var systemType = appSettings.SystemType ?? string.Empty;

                if (betCreated != null)
                {
                    await highStakeServices.NotifyHighStake(betCreated, systemType);
                }

                return true;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return true;
            }
        }
    }
}