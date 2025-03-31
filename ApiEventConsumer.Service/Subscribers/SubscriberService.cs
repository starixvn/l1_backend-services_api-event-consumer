using NATS.Client;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace ApiEventConsumer.Subscribers
{
    public interface ISubscriberService
    {
        void InitializeNatsConnection();
    }

    public abstract class SubscriberService : ISubscriberService, IHostedService
    {
        protected readonly string baseCode;
        protected readonly ISentryClient sentryClient;
        protected readonly IConfiguration _configuration;
        
        private IConnection? _connection;
        private IAsyncSubscription? _betCreatedSub;
        private IAsyncSubscription? _betSettledSub;

        protected SubscriberService(
            string baseCode, 
            ISentryClient sentryClient,
            IConfiguration configuration)
        {
            this.baseCode = baseCode;
            this.sentryClient = sentryClient;
            _configuration = configuration;
        }

        protected abstract string GetStreamSettingsPath();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            InitializeNatsConnection();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            await Task.CompletedTask;
        }

        public void InitializeNatsConnection()
        {
            try
            {
                var streamSettings = _configuration.GetSection(GetStreamSettingsPath());
                var isEnabled = streamSettings.GetValue<bool>("Enable");

                if (!isEnabled) return;

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = streamSettings.GetValue<string>("Host");
                options.DisconnectedEventHandler = (sender, args) =>
                {
                    sentryClient.CaptureException(new Exception($"Disconnected from NATS server: {args.Error}"));
                };

                _connection = new ConnectionFactory().CreateConnection(options);

                if (_connection.State != ConnState.CONNECTED)
                {
                    sentryClient.CaptureException(new Exception($"Failed to connect to NATS server. State: {_connection.State}"));
                    return;
                }

                _betCreatedSub = _connection.SubscribeAsync($"api.bet.created", HandleBetCreated);
                _betCreatedSub.Start();

                _betSettledSub = _connection.SubscribeAsync($"api.bet.settled", HandleBetSettled);
                _betSettledSub.Start();
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Critical error in NATS initialization: {ex}"));
            }
        }

        protected virtual void HandleBetCreated(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                // Handle bet created message
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Error processing bet.created message: {ex}"));
            }
        }

        protected virtual void HandleBetSettled(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                // Handle bet settled message
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Error processing bet.settled message: {ex}"));
            }
        }

        public void Dispose()
        {
            try
            {
                _betCreatedSub?.Unsubscribe();
                _betSettledSub?.Unsubscribe();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Error disposing NATS connections: {ex}"));
            }
        }
    }
}