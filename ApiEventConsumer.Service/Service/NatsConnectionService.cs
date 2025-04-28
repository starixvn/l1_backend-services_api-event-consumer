using ApiEventConsumer._Shared.Configurations;
using NATS.Client;
using NATS.Client.JetStream;

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
                const int maxReconnect = 10;
                const int reconnectWait = 10000;

                var streamSettings = appSettings.StreamSettings;
                if (streamSettings?.Enable != true)
                {
                    return;
                }

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = streamSettings.Host;
                options.AllowReconnect = true;
                options.MaxReconnect = maxReconnect;
                options.ReconnectWait = reconnectWait;
                options.DisconnectedEventHandler += (_, _) =>
                {
                    sentryClient.CaptureException(new Exception("Disconnected from NATS server - attempting to reconnect"));
                };
                options.ReconnectedEventHandler += (_, _) =>
                {
                    sentryClient.CaptureException(new Exception("Successfully reconnected to NATS server"));
                };
                options.ClosedEventHandler += (_, _) =>
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
                    .WithDeliverPolicy(DeliverPolicy.All)
                    .WithMaxAckPending(50)
                    .WithMaxDeliver(10)
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
                if (args.Message.MetaData == null)
                {
                    args.Message.Nak();
                    return;
                }

                try
                {
                    bool success = await handler(sender, args);
                    if (!success)
                    {
                        args.Message.Nak();
                        return;
                    }

                    await ProcessSuccessfulMessage(args, streamName, jsManagement);
                }
                catch (Exception ex)
                {
                    sentryClient.CaptureException(ex);
                    args.Message.Nak();
                }
            }

            subscription = jsContext.PushSubscribeAsync(subject, autoAckHandler, false, subscribeOptions);
            subscription.Start();
        }

        private async Task ProcessSuccessfulMessage(MsgHandlerEventArgs args, string streamName, IJetStreamManagement jsManagement)
        {
            try
            {
                args.Message.Ack();
                await Task.Run(() => jsManagement.DeleteMessage(streamName, args.Message.MetaData.StreamSequence));
            }
            catch (Exception deleteEx)
            {
                sentryClient.CaptureException(deleteEx);
            }
        }

        public void Disconnect()
        {
            subscription?.Unsubscribe();
            connection?.Dispose();
        }
    }
}