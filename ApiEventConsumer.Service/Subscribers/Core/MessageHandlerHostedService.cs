using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Service.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ApiEventConsumer.Subscribers
{
    public class MessageHandlerHostedService : IHostedService
    {
        private readonly INatsConnectionService natsConnection;
        private readonly ISubscriberService subscriberService;
        private readonly IAppSettings appSettings;
        private readonly IEnumerable<IEventSubscriber> subscribers;

        public MessageHandlerHostedService(
            INatsConnectionService natsConnection,
            ISubscriberService subscriberService,
            IAppSettings appSettings,
            IEnumerable<IEventSubscriber> subscribers)
        {
            this.natsConnection = natsConnection;
            this.subscriberService = subscriberService;
            this.appSettings = appSettings;
            this.subscribers = subscribers;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Initialize NATS connection
            natsConnection.InitializeConnection();
            
            // For backward compatibility - use the legacy JetStream approach
            if (appSettings?.StreamSettings?.BetMessages != null)
            {
                natsConnection.ConfigureJetStream(subscriberService.HandleBetCreated, appSettings.StreamSettings.BetMessages);
            }
            
            // With new pattern, subscribers self-register via IConnection in their Subscribe method
            // This is automatically handled by the SubscriberManager during registration
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            natsConnection.Disconnect();
            return Task.CompletedTask;
        }
    }
} 