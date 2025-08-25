using ApiEventConsumer._Shared.Configurations;
using NATS.Client;
using NATS.Client.JetStream;

namespace ApiEventConsumer.Services
{
    public interface INatsConnectionService
    {
        void InitializeConnection();
        void ConfigureJetStream(Func<object?, MsgHandlerEventArgs, Task<bool>> messageHandler, string subject);
        Task DisconnectAsync();
        bool IsHealthy();
    }

    public class NatsConnectionService : INatsConnectionService
    {
        private const string PullConsumerSuffix = "_PULL";
        private const string ConsumerSuffix = "_CONSUMER";

        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private IConnection? connection;
        private readonly List<IJetStreamPullSubscription> subscriptions;
        private readonly List<CancellationTokenSource> cancellationTokenSources;

        public NatsConnectionService(ISentryClient sentryClient, IAppSettings appSettings)
        {
            this.sentryClient = sentryClient;
            this.appSettings = appSettings;
            subscriptions = [];
            cancellationTokenSources = [];
        }

        public void InitializeConnection()
        {
            try
            {
                var streamSettings = appSettings.StreamSettings;
                if (!streamSettings.Enable)
                {
                    return;
                }

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = streamSettings.Host;
                options.AllowReconnect = true;
                options.MaxReconnect = streamSettings.MaxReconnectAttempts;
                options.ReconnectWait = streamSettings.ReconnectWaitMs;
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
                if (connection == null || streamSettings.StreamName == null || subject == null)
                {
                    return;
                }

                var jsContext = connection.CreateJetStreamContext();
                ConfigureSubscription(jsContext, streamSettings.StreamName, subject, messageHandler);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private void ConfigureSubscription(IJetStream jsContext, string streamName, string subject, Func<object?, MsgHandlerEventArgs, Task<bool>> handler)
        {
            var consumerName = $"{GetConsumerName(subject)}{PullConsumerSuffix}";
            var consumerSettings = appSettings.StreamSettings.ConsumerSettings;

            var consumerConfig = ConsumerConfiguration.Builder()
                    .WithName(consumerName)
                    .WithAckWait(consumerSettings.AckWaitMs)
                    .WithDeliverPolicy(DeliverPolicy.All)
                    .WithMaxAckPending(consumerSettings.MaxAckPending)
                    .WithMaxDeliver(consumerSettings.MaxDeliver)
                    .WithFilterSubject(subject)
                    .WithAckPolicy(AckPolicy.Explicit)
                    .WithReplayPolicy(ReplayPolicy.Instant)
                    .Build();

            if (connection == null)
            {
                return;
            }

            var jsManagement = connection.CreateJetStreamManagementContext();
            try
            {
                jsManagement.AddOrUpdateConsumer(streamName, consumerConfig);
            }
            catch (NATSJetStreamException ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }

            var subscribeOptions = PullSubscribeOptions.Builder()
                    .WithStream(streamName)
                    .WithConfiguration(consumerConfig)
                    .Build();

            var subscription = jsContext.PullSubscribe(subject, subscribeOptions);
            subscriptions.Add(subscription);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSources.Add(cancellationTokenSource);

            _ = Task.Run(async () => await StartMessagePullingLoop(subscription, handler, cancellationTokenSource.Token));
        }

        private async Task StartMessagePullingLoop(IJetStreamPullSubscription subscription, Func<object?, MsgHandlerEventArgs, Task<bool>> handler, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await PullAndProcessMessages(subscription, handler, cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                sentryClient.CaptureMessage($"Message pulling loop cancelled for subscription: {ex.Message}");
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

        private async Task PullAndProcessMessages(IJetStreamPullSubscription subscription, Func<object?, MsgHandlerEventArgs, Task<bool>> handler, CancellationToken cancellationToken)
        {
            var consumerSettings = appSettings.StreamSettings.ConsumerSettings;

            try
            {
                subscription.Pull(consumerSettings.PullBatchSize);
                var messages = subscription.Fetch(consumerSettings.PullBatchSize, consumerSettings.PullTimeoutMs);

                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await ProcessPulledMessage(message, handler);
                }

                if (messages.Count == 0)
                {
                    await Task.Delay(consumerSettings.NoMessageDelayMs, cancellationToken);
                }
            }
            catch (NATSTimeoutException ex)
            {
                sentryClient.CaptureMessage($"NATS fetch timeout - no messages available: {ex.Message}", SentryLevel.Debug);
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                await Task.Delay(consumerSettings.ErrorRetryDelayMs, cancellationToken);
            }
        }

        private async Task ProcessPulledMessage(Msg message, Func<object?, MsgHandlerEventArgs, Task<bool>> handler)
        {
            try
            {
                if (message.MetaData == null)
                {
                    message.Nak();
                    return;
                }

                var args = new MsgHandlerEventArgs(message);
                bool success = await handler(null, args);

                if (!success)
                {
                    message.Nak();
                    return;
                }

                message.Ack();
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                try
                {
                    message.Nak();
                }
                catch (Exception ackEx)
                {
                    sentryClient.CaptureException(ackEx);
                }
            }
        }

        private static string GetConsumerName(string subject)
        {
            var cleanSubject = subject.Replace(".", "_").Replace("*", "ALL").ToUpperInvariant();
            return $"{cleanSubject}{ConsumerSuffix}";
        }

        public bool IsHealthy()
        {
            try
            {
                if (connection == null || connection.IsClosed())
                {
                    return false;
                }

                if (subscriptions.Count == 0 || subscriptions.Any(s => s == null || !s.IsValid))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                int shutdownWaitMs = appSettings.StreamSettings.ShutdownWaitMs;

                foreach (var cancellationTokenSource in cancellationTokenSources)
                {
                    try
                    {
                        cancellationTokenSource?.Cancel();
                    }
                    catch (Exception ex)
                    {
                        sentryClient.CaptureException(ex);
                    }
                }

                await Task.Delay(shutdownWaitMs);

                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        subscription?.Unsubscribe();
                    }
                    catch (Exception ex)
                    {
                        sentryClient.CaptureException(ex);
                    }
                }

                subscriptions.Clear();

                foreach (var cancellationTokenSource in cancellationTokenSources)
                {
                    try
                    {
                        cancellationTokenSource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        sentryClient.CaptureException(ex);
                    }
                }

                cancellationTokenSources.Clear();
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
            finally
            {
                connection?.Dispose();
            }
        }
    }
}