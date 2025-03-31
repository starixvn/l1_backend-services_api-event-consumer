using NATS.Client;
using System.Text;

namespace ApiEventConsumer.Service.Subscribers
{
    public class NatsService : IHostedService
    {
        private IConnection _connection;
        private IAsyncSubscription _subscription;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NatsService> _logger;

        public NatsService(IConfiguration configuration, ILogger<NatsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var streamSettings = _configuration.GetSection("StreamSettings");
            var isEnabled = streamSettings.GetValue<bool>("Enable");
            
            if (!isEnabled)
            {
                _logger.LogInformation("NATS streaming is disabled");
                return Task.CompletedTask;
            }

            var options = ConnectionFactory.GetDefaultOptions();
            options.Url = streamSettings.GetValue<string>("Host");
            
            try 
            {
                _connection = new ConnectionFactory().CreateConnection(options);
                if (_connection.State == ConnState.CONNECTED)
                {
                    _logger.LogInformation("Connected to NATS server at {0}", options.Url);
                }
                else 
                {
                    _logger.LogError("Failed to connect to NATS server. State: {0}", _connection.State);
                    return Task.CompletedTask;
                }

                var prefix = streamSettings.GetValue<string>("Prefix");
                
                // Subscribe to api.bet.created events
                _subscription = _connection.SubscribeAsync($"{prefix}.api.bet.created", (sender, args) =>
                {
                    var message = Encoding.UTF8.GetString(args.Message.Data);
                    _logger.LogInformation($"New bet created: {message}");
                });
                _subscription.Start();

                // Subscribe to api.bet.settled events
                var settledSubscription = _connection.SubscribeAsync($"{prefix}.api.bet.settled", (sender, args) =>
                {
                    var message = Encoding.UTF8.GetString(args.Message.Data);
                    _logger.LogInformation($"Bet settled: {message}");
                });
                settledSubscription.Start();

                options.DisconnectedEventHandler = (sender, args) =>
                {
                    _logger.LogError("Disconnected from NATS server: {0}", args.Error);
                };

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to NATS server");
                return Task.CompletedTask;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _subscription?.Unsubscribe();
            _connection?.Dispose();
            return Task.CompletedTask;
        }
    }
}