using ApiEventConsumer._Shared.Configurations;
using NATS.Client;
using NATS.Client.JetStream;
using Polly;
using Sentry;

namespace ApiEventConsumer.Service.Service
{
    public interface INatsConnectionService
    {
        void InitializeConnection();
        void ConfigureJetStreamBetMessage(Func<object?, MsgHandlerEventArgs, Task<bool>> messageHandler);
        void Disconnect();
    }

    public class NatsConnectionService : INatsConnectionService
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private IConnection? connection;
        private Func<object?, MsgHandlerEventArgs, Task<bool>>? storedHandler;
        private string? currentConsumerId;
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
                options.DisconnectedEventHandler = async (_, args) =>
                {
                    sentryClient.CaptureException(new Exception($"Disconnected from NATS server: {args.Error}"));
                    await TryReconnectAsync(options);
                };


                connection = new ConnectionFactory().CreateConnection(options);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }
        }

        public void ConfigureJetStreamBetMessage(Func<object?, MsgHandlerEventArgs, Task<bool>> messageHandler)
        {
            try
            {
                storedHandler = messageHandler;
                var streamSettings = appSettings.StreamSettings;
                if (connection == null || streamSettings?.StreamName == null || streamSettings.BetMessages == null)
                {
                    return;
                }

                var jsContext = connection.CreateJetStreamContext();
                var jsManagement = connection.CreateJetStreamManagementContext();

                ConfigureStream(jsManagement, streamSettings.StreamName);
                ConfigureSubscription(jsContext, streamSettings.StreamName, streamSettings.BetMessages, messageHandler);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private async Task TryReconnectAsync(Options options)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)),
                    onRetry: (_, _, retryCount, _) =>
                    {
                        sentryClient.CaptureException(
                            new Exception($"Reconnection attempt {retryCount} failed"));
                    });

            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    if (await Task.FromResult(AttemptReconnectionAsync(options)) && storedHandler != null)
                    {
                        CleanupOldSubscriptionAndStream();
                        ConfigureJetStreamBetMessage(storedHandler);
                        sentryClient.CaptureMessage("Successfully reconfigured JetStream after reconnection");
                        return;
                    }
                    throw new InvalidOperationException("Connection attempt failed");
                });
            }
            catch (Exception)
            {
                sentryClient.CaptureException(
                    new Exception("Failed to reconnect to NATS server after maximum retries"));
            }
        }

        private void CleanupOldSubscriptionAndStream()
        {
            try
            {
                if (connection?.State == ConnState.CONNECTED &&
                    !string.IsNullOrEmpty(currentConsumerId) &&
                    !string.IsNullOrEmpty(appSettings?.StreamSettings?.StreamName))
                {
                    var jsManagement = connection.CreateJetStreamManagementContext();

                    try
                    {
                        jsManagement.DeleteConsumer(
                            appSettings.StreamSettings.StreamName,
                            currentConsumerId);
                    }
                    catch (NATSJetStreamException ex)
                    {
                        sentryClient.CaptureMessage($"Error deleting old consumer: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception("Error cleaning up old subscription", ex));
            }
            finally
            {
                currentConsumerId = null;
            }
        }

        private bool AttemptReconnectionAsync(Options options)
        {
            connection = new ConnectionFactory().CreateConnection(options);

            if (connection.State == ConnState.CONNECTED)
            {
                return true;
            }

            return false;
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
            currentConsumerId = $"consumer_{Guid.NewGuid():N}";
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

            var subscription = jsContext.PushSubscribeAsync(subject, autoAckHandler, false, subscribeOptions);
            subscription.Start();
        }

        public void Disconnect()
        {
            Task.Run(CleanupOldSubscriptionAndStream).Wait();
            connection?.Dispose();
        }
    }
}