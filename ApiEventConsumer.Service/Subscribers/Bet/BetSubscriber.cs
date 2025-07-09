using System;
using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Service.Service;
using NATS.Client;
using System.Threading.Tasks;

namespace ApiEventConsumer.Subscribers.Bet
{
    public class BetSubscriber : IEventSubscriber
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private readonly IHighStakeServices highStakeServices;


        public BetSubscriber(
            ISentryClient sentryClient,
            IHighStakeServices highStakeServices,
            ConfigurationServices configurations)
        {
            this.sentryClient = sentryClient;
            this.appSettings = configurations.AppSettings;
            this.highStakeServices = highStakeServices;
        }

        public string SubjectName => appSettings?.StreamSettings?.BetMessages ?? string.Empty;
        public async Task<bool> OnMessageReceived(object sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                var systemType = appSettings.SystemType ?? string.Empty;

                if (betCreated != null && (double)betCreated.Amount >= appSettings.HighStake)
                {
                    await highStakeServices.NotifyHighStake(betCreated, systemType);
                }
                
                return true; // Processing successful
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return false; // Processing failed
            }
        }
    }
} 