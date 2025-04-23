using NATS.Client;
using ApiEventConsumer._Shared.Configurations;
using System.Text.Json;
using Refit;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Core.Models.ChatBot;
using ApiEventConsumer.Core.Models.NatMessage;
using Sentry;
using System.Text;

namespace ApiEventConsumer.Subscribers
{
    public interface IChatBotApi
    {
        [Post("/forwardwithtoken")]
        Task<ChatBotResult> ForwardMessage(string token, string conversationId, string message);
    }

    public interface ISubscriberService
    {
        Task<bool> HandleBetCreated(object? sender, MsgHandlerEventArgs args);
    }

    public class SubscriberServices : ISubscriberService
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private readonly IHighStakeServices highStakeServices;


        public SubscriberServices(
            ISentryClient sentryClient,
            IHighStakeServices highStakeServices,
            ConfigurationServices configurations)
        {
            this.sentryClient = sentryClient;
            this.appSettings = configurations.AppSettings;
            this.highStakeServices = highStakeServices;
        }


        public async Task<bool> HandleBetCreated(object? sender, MsgHandlerEventArgs args)
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
                return true;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return false;
            }
        }
    }
}