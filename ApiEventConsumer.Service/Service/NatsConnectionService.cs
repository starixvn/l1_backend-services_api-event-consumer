using ApiEventConsumer._Shared.Configurations;
using NATS.Client;
using NATS.Client.JetStream;
using NATS.Client.Internals;
using Sentry;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiEventConsumer.Service.Service
{
    public interface INatsConnectionService
    {
        void InitializeConnection();
        void ConfigureJetStream(Func<object?, MsgHandlerEventArgs, Task<bool>> messageHandler, string subject);
        void Disconnect();
        bool IsHealthy();
    }

    public class NatsConnectionService(
        ISentryClient sentryClient,
        IAppSettings appSettings) : INatsConnectionService
    {
        // Connection constants
        private const int MaxReconnectAttempts = 10;
        private const int ReconnectWaitMs = 10000;
        
        // Consumer configuration constants
        private const int ConsumerAckWaitMs = 30000;
        private const int ConsumerMaxAckPending = 50;
        private const int ConsumerMaxDeliver = 10;
        
        // Pull subscription constants
        private const int PullBatchSize = 10;
        private const int PullTimeoutMs = 1000;
        private const int NoMessageDelayMs = 100;
        private const int ErrorRetryDelayMs = 1000;
        
        // Shutdown constants
        private const int ShutdownWaitMs = 500;
        
        // Consumer naming constants
        private const string PullConsumerSuffix = "_PULL";
        private const string ConsumerSuffix = "_CONSUMER";

        private readonly ISentryClient sentryClient = sentryClient;
        private readonly IAppSettings appSettings = appSettings;
        private IConnection? connection;
        private readonly List<IJetStreamPullSubscription> subscriptions = new();
        private readonly List<CancellationTokenSource> cancellationTokenSources = new();
        private string? currentConsumerName;
        private string? currentStreamName;

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
                options.MaxReconnect = MaxReconnectAttempts;
                options.ReconnectWait = ReconnectWaitMs;
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

            currentConsumerName = consumerName;
            currentStreamName = streamName;

            var consumerConfig = ConsumerConfiguration.Builder()
                    .WithName(consumerName)
                    .WithAckWait(ConsumerAckWaitMs)
                    .WithDeliverPolicy(DeliverPolicy.All)
                    .WithMaxAckPending(ConsumerMaxAckPending)
                    .WithMaxDeliver(ConsumerMaxDeliver)
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
                // With PULL consumers, we can simply create or update the consumer
                // The AddOrUpdateConsumer method is idempotent and will handle existing consumers
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

            // Start the message pulling loop
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
                    try
                    {
                        // Fetch messages in batches to improve performance
                        subscription.Pull(PullBatchSize);
                        
                        // Wait for messages to arrive
                        var messages = subscription.Fetch(PullBatchSize, PullTimeoutMs);
                        
                        foreach (var message in messages)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            await ProcessPulledMessage(message, handler);
                        }
                        
                        // If no messages were received, wait a bit before trying again
                        if (messages.Count == 0)
                        {
                            await Task.Delay(NoMessageDelayMs, cancellationToken);
                        }
                    }
                    catch (NATSTimeoutException)
                    {
                        // Timeout is expected when no messages are available, continue
                        continue;
                    }
                    catch (Exception ex)
                    {
                        sentryClient.CaptureException(ex);
                        // Wait before retrying to avoid tight error loops
                        await Task.Delay(ErrorRetryDelayMs, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
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

        private string GetConsumerName(string subject)
        {
            var cleanSubject = subject.Replace(".", "_").Replace("*", "ALL").ToUpper();
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

                if (currentStreamName != null && currentConsumerName != null)
                {
                    var jsManagement = connection.CreateJetStreamManagementContext();
                    var consumerInfo = jsManagement.GetConsumerInfo(currentStreamName, currentConsumerName);
                    return consumerInfo != null;
                }

                return true;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return false;
            }
        }



        public void Disconnect()
        {
            try
            {
                // Cancel all pulling loops
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

                // Wait a bit for pulling loops to finish
                Task.Delay(ShutdownWaitMs).Wait();

                // Unsubscribe from all subscriptions
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

                // Dispose cancellation token sources
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
                currentConsumerName = null;
                currentStreamName = null;
            }
        }
    }
}