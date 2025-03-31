using NATS.Client;
using System.Text;
using Sentry;

namespace ApiEventConsumer.Service.Subscribers
{
    public class NatsService(IConfiguration configuration) : BackgroundService
    {
        private IConnection? _connection;
        private IAsyncSubscription? _betCreatedSub;
        private IAsyncSubscription? _betSettledSub;
        private readonly IConfiguration _configuration = configuration;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var streamSettings = _configuration.GetSection("StreamSettings");
            var isEnabled = streamSettings.GetValue<bool>("Enable");

            if (!isEnabled)
            {
                return;
            }

            try
            {
                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = streamSettings.GetValue<string>("Host");
                options.DisconnectedEventHandler = (sender, args) =>
                {
                    SentrySdk.CaptureException(new Exception($"Disconnected from NATS server: {args.Error}"));
                };

                _connection = new ConnectionFactory().CreateConnection(options);

                if (_connection.State != ConnState.CONNECTED)
                {
                    SentrySdk.CaptureException(new Exception($"Failed to connect to NATS server. State: {_connection.State}"));
                    return;
                }

                var prefix = streamSettings.GetValue<string>("Prefix");

                // Subscribe to bet.created events
                _betCreatedSub = _connection.SubscribeAsync($"api.bet.created", (sender, args) =>
                {
                    try 
                    {
                        var message = Encoding.UTF8.GetString(args.Message.Data);
                    }
                    catch (Exception ex)
                    {
                        SentrySdk.CaptureException(new Exception($"Error processing bet.created message: {ex}"));
                    }
                });
                _betCreatedSub.Start();

                // Subscribe to bet.settled events
                _betSettledSub = _connection.SubscribeAsync($"api.bet.settled", (sender, args) =>
                {
                    try 
                    {
                        var message = Encoding.UTF8.GetString(args.Message.Data);
                    }
                    catch (Exception ex)
                    {
                        SentrySdk.CaptureException(new Exception($"Error processing bet.settled message: {ex}"));
                    }
                });
                _betSettledSub.Start();

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(new Exception($"Critical error in NATS service: {ex}"));
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _betCreatedSub?.Unsubscribe();
                _betSettledSub?.Unsubscribe();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(new Exception($"Error stopping NATS service: {ex}"));
            }

            await base.StopAsync(cancellationToken);
        }
    }
}