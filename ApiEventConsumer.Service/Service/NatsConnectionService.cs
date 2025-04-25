using ApiEventConsumer._Shared.Configurations;
using NATS.Client;
using NATS.Client.JetStream;
using Polly;

namespace ApiEventConsumer.Service.Service
{
    public interface INatsConnectionService
    {
        void InitializeConnection();
        void ConfigureJetStream(Func<object?, MsgHandlerEventArgs, Task<bool>> messageHandler, string subject);
        void Disconnect();
    }

    public class NatsConnectionService : INatsConnectionService
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private IConnection? connection;
        private IAsyncSubscription? subscription;
        public NatsConnectionService(
            ISentryClient sentryClient,
            IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.appSettings = appSettings;
        }

        public void InitializeConnection()
        {
            try
            {
                var streamSettings = appSettings.StreamSettings;
                if (streamSettings?.Enable != true)
                {
                    return;
                }

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = streamSettings.Host;
                options.AllowReconnect = true;
                options.MaxReconnect = 10;
                options.ReconnectWait = 10000;
                options.DisconnectedEventHandler += (_, args) =>
                {
                    sentryClient.CaptureException(new Exception("Disconnected from NATS server - attempting to reconnect"));
                };
                options.ReconnectedEventHandler += (_, args) =>
                {
                    sentryClient.CaptureException(new Exception("Successfully reconnected to NATS server"));
                };
                options.ClosedEventHandler += (_, args) =>
                {
                    sentryClient.CaptureException(new Exception("The NATS server connection has been closed"));
                };

                connection = new ConnectionFactory().CreateConnection(options);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }
        }

        public void ConfigureJetStream(Func<object?, MsgHandlerEventArgs, Task<bool>> messageHandler, string subject)
        {
            try
            {
                var streamSettings = appSettings.StreamSettings;
                if (connection == null || streamSettings?.StreamName == null || subject == null)
                {
                    return;
                }

                var jsContext = connection.CreateJetStreamContext();
                var jsManagement = connection.CreateJetStreamManagementContext();

                ConfigureStream(jsManagement, streamSettings.StreamName);
                ConfigureSubscription(jsContext, streamSettings.StreamName, subject, messageHandler);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private void ConfigureStream(IJetStreamManagement jsManagement, string streamName)
        {
            var streamConfig = StreamConfiguration.Builder()
                     .WithName(streamName)
                     .WithSubjects($"{streamName}.*")
                     .WithStorageType(StorageType.Memory)
                     .WithRetentionPolicy(RetentionPolicy.WorkQueue)
                     .Build();

            try
            {
                var streamInfo = jsManagement.GetStreamInfo(streamName);
                if (streamInfo == null)
                {
                    jsManagement.AddStream(streamConfig);
                }
            }
            catch (NATSJetStreamException ex)
            {
                sentryClient.CaptureException(ex);
            }

        }

        private void ConfigureSubscription(IJetStream jsContext, string streamName, string subject, Func<object?, MsgHandlerEventArgs, Task<bool>> handler)
        {
            var currentConsumerId = $"consumer_{Guid.NewGuid():N}";
            var consumerConfig = ConsumerConfiguration.Builder()
                    .WithName(currentConsumerId)
                    .WithDurable(currentConsumerId)
                    .WithAckWait(30000)
                    .WithDeliverPolicy(DeliverPolicy.New)
                    .WithIdleHeartbeat(5000)
                    .WithMaxAckPending(5)
                    .WithMaxDeliver(10)
                    .WithMaxAckPending(100)
                    .WithFilterSubject(subject)
                    .Build();

            var subscribeOptions = PushSubscribeOptions.Builder()
                .WithStream(streamName)
                .WithConfiguration(consumerConfig)
                .Build();

            if (connection == null)
            {
                return;
            }
            var jsManagement = connection.CreateJetStreamManagementContext();

            async void autoAckHandler(object? sender, MsgHandlerEventArgs args)
            {
                try
                {
                    bool success = await handler(sender, args);
                    args.Message.Ack();
                    if (success && args.Message.MetaData != null)
                    {
                        jsManagement.DeleteMessage(streamName, args.Message.MetaData.StreamSequence);
                    }
                    else
                    {
                        args.Message.Nak();
                    }
                }
                catch (Exception)
                {
                    args.Message.Nak();
                    throw;
                }
            }

            subscription = jsContext.PushSubscribeAsync(subject, autoAckHandler, false, subscribeOptions);
            subscription.Start();
        }

        public void Disconnect()
        {
            subscription?.Unsubscribe();
            connection?.Dispose();
        }
    }
}