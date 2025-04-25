using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Service.Service;
using NATS.Client;

namespace ApiEventConsumer.Subscribers.Bet
{
    public class BetSubscriber : IEventSubscriber
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private readonly IHighStakeServices highStakeServices;

        public string SubjectName => "bet.created";

        public BetSubscriber(
            ISentryClient sentryClient,
            IHighStakeServices highStakeServices,
            ConfigurationServices configurations)
        {
            this.sentryClient = sentryClient;
            this.appSettings = configurations.AppSettings;
            this.highStakeServices = highStakeServices;
        }

        public void OnMessageReceived(object sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                var systemType = appSettings.SystemType ?? string.Empty;

                if (betCreated != null && (double)betCreated.Amount >= appSettings.HighStake)
                {
                    // Use Task.Run to handle async operation in void method
                    Task.Run(async () => {
                        await highStakeServices.NotifyHighStake(betCreated, systemType);
                    });
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }
    }
} 