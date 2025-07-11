using ApiEventConsumer._Shared.Configurations;
using NATS.Client;
using NATS.Client.JetStream;
using NATS.Client.Internals;
using Sentry;
using System.Linq;

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
        private readonly ISentryClient sentryClient = sentryClient;
        private readonly IAppSettings appSettings = appSettings;
        private IConnection? connection;
        private IAsyncSubscription? subscription;
        private string? currentConsumerName;
        private string? currentStreamName;

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
            var streamSettings = appSettings.StreamSettings;
            
            var subjectPrefix = streamSettings?.SubjectPrefix ?? streamName;
            
            var streamConfig = StreamConfiguration.Builder()
                     .WithName(streamName)
                     .WithSubjects($"{subjectPrefix}.>")
                     .WithMaxAge(Duration.OfHours(streamSettings?.MaxAgeHours ?? 24))
                     .WithDiscardPolicy(DiscardPolicy.Old)
                     .WithRetentionPolicy(RetentionPolicy.Limits)
                     .WithMaxMessages(streamSettings?.MaxMessages ?? 1000000)
                     .WithStorageType(StorageType.File)
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
            var consumerName = GetConsumerName(subject);
            
            currentConsumerName = consumerName;
            currentStreamName = streamName;

            var consumerConfig = ConsumerConfiguration.Builder()
                    .WithName(consumerName)
                    .WithDurable(consumerName)
                    .WithAckWait(30000)
                    .WithDeliverPolicy(DeliverPolicy.All)
                    .WithMaxAckPending(50)
                    .WithMaxDeliver(10)
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
                var existingConsumer = GetExistingConsumer(jsManagement, streamName, consumerName);
                
                if (existingConsumer != null && !IsConsumerCompatible(existingConsumer))
                {
                    sentryClient.CaptureMessage($"Deleting incompatible consumer: {consumerName}");
                    jsManagement.DeleteConsumer(streamName, consumerName);
                }

                jsManagement.AddOrUpdateConsumer(streamName, consumerConfig);
            }
            catch (NATSJetStreamException ex)
            {
                sentryClient.CaptureException(ex);
                throw;
            }

            var subscribeOptions = PushSubscribeOptions.Builder()
                    .WithStream(streamName)
                    .WithConfiguration(consumerConfig)
                    .Build();

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

                    ProcessSuccessfulMessage(args);
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

        private string GetConsumerName(string subject)
        {
            var cleanSubject = subject.Replace(".", "_").Replace("*", "ALL").ToUpper();
            return $"{cleanSubject}_CONSUMER";
        }

        private ConsumerInfo? GetExistingConsumer(IJetStreamManagement jsManagement, string streamName, string consumerName)
        {
            try
            {
                return jsManagement.GetConsumerInfo(streamName, consumerName);
            }
            catch (NATSJetStreamException ex) when (ex.Message.Contains("consumer not found") || ex.Message.Contains("10014"))
            {
                return null;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return null;
            }
        }

        private bool IsConsumerCompatible(ConsumerInfo consumerInfo)
        {
            try
            {
                sentryClient.CaptureMessage($"Recreating consumer {consumerInfo.Name} to ensure push consumer compatibility");
                return false;
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
                return false;
            }
        }

        public bool IsHealthy()
        {
            try
            {
                if (connection == null || connection.IsClosed())
                {
                    return false;
                }

                if (subscription == null || !subscription.IsValid)
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

        private void ProcessSuccessfulMessage(MsgHandlerEventArgs args)
        {
            try
            {
                args.Message.Ack();
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }

                public void Disconnect()
        {
            try
            {
                subscription?.Unsubscribe();
                
                if (connection != null && currentStreamName != null && currentConsumerName != null)
                {
                    try
                    {
                        var jsManagement = connection.CreateJetStreamManagementContext();
                    }
                    catch (Exception ex)
                    {
                        sentryClient.CaptureException(ex);
                    }
                }
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