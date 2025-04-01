using NATS.Client;
using System.Text;
using Microsoft.Extensions.Hosting;
using ApiEventConsumer._Shared.Configurations;
using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ApiEventConsumer.Service.Subscribers;

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
        protected readonly ILogger<SubscriberService> _logger;
        
        private IConnection? _connection;
        private IAsyncSubscription? _betCreatedSub;
        private IAsyncSubscription? _betSettledSub;

        private const string HighStakeMessageTemplate = "**{0}** - {1} - {2} - {3} - {4} {5}";
        private const double DefaultHighStakeAmount = 2000;

        private readonly IChatBotApi chatBotApi;
        private readonly IAppSettings appSettings;

        protected SubscriberService(
            string baseCode, 
            ISentryClient sentryClient,
            IConfiguration configuration,
            ILogger<SubscriberService> logger,
            IChatBotApi chatBotApi,
            IAppSettings appSettings)
        {
            this.baseCode = baseCode;
            this.sentryClient = sentryClient;
            _configuration = configuration;
            _logger = logger;
            this.chatBotApi = chatBotApi;
            this.appSettings = appSettings;
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
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Critical error in NATS initialization: {ex}"));
            }
        }

        public async Task<ChatBotResult> NotifyHighStake(BetCreated bet)
        {
            try
            {
                var highStakeAmount = GetHighStakeAmount();
                var amount = await GetAmount(bet);

                if (bet.Currency == "UUS" || amount < highStakeAmount)
                {
                    return await Task.FromResult<ChatBotResult>(default);
                }

                var msgBuilder = new StringBuilder();
                var game = await GetGameName(bet.GameTypeId);
                var charName = await GetCharName(bet);
                //var version = GetVersionName(Enum.Parse<Versions>(bet.System.ToString()), bet.Metadata);
                

                var playerDeviceExtraInformation = await GetDeviceInformation(bet.PlayerId.ToString());
                msgBuilder.Append(string.Format(HighStakeMessageTemplate, "", charName, bet.Amount, bet.Currency, game, playerDeviceExtraInformation));

                _ = Task.Run(() => chatBotApi.ForwardMessage(appSettings.ChatBotApiSettings.Token, appSettings.ChatBotApiSettings.ConversationIds.HighStake, msgBuilder.ToString()));

                return await Task.FromResult(new ChatBotResult());
            }
            catch (Exception e)
            {
                sentryClient.CaptureException(e);
                return await Task.FromResult(new ChatBotResult());
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

        protected virtual void HandleBetCreated(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                
                if (betCreated != null)
                {
                    // Handle bet created message
                    _logger.LogInformation("Bet created: GameRoundId={GameRoundId}, Amount={Amount}, Currency={Currency}", 
                        betCreated.GameRoundId, betCreated.Amount, betCreated.Currency);
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Error processing bet.created message: {ex}"));
            }
        }

        private double GetHighStakeAmount()
        {
            return appSettings.CloudSettings.IsCloudService ? DefaultHighStakeAmount : appSettings.ChatBotApiSettings.HighStakeAmount;
        }

        private async Task<double> GetAmount(BetCreated bet)
        {
            return appSettings.CloudSettings.IsCloudService ? (double)bet.Amount : await CalAmountOnBaseCurrency(bet);
        }

        private async Task<double> CalAmountOnBaseCurrency(BetCreated bet)
        {
            try
            {
                var exchangeRate = await GetExchangeRate();
                var currencies = await GetCurrencies();
                var user = await GetUser(bet.PlayerId.ToString());
                var currency = currencies.FirstOrDefault(c => c.Enabled && c.Name == bet.Currency && c.PartnerId == user.OperatorId);

                if (currency == null)
                {
                    return (double)bet.Amount;
                }

                var rate = (double)exchangeRate.Code[currency.Code] * (double)currency.Multiplier;
                return (double)bet.Amount * rate;
            }
            catch (Exception e)
            {
                sentryClient.CaptureException(e);
                return (double)bet.Amount;
            }
        }

        private async Task<string> GetGameName(int gameTypeId)
        {
            // In real implementation, this would get game name from a game service
            return $"Game_{gameTypeId}";
        }

        private async Task<string> GetCharName(BetCreated bet)
        {
            // In real implementation, this would get character name from user service
            return $"Player_{bet.PlayerId}";
        }

        private string GetVersionName(Versions version, string metadata)
        {
            return version switch
            {
                Versions.API => "API",
                Versions.WEB => "WEB",
                Versions.MOBILE => "MOBILE",
                _ => "UNKNOWN"
            };
        }

        private async Task<string> GetDeviceInformation(string obCustId)
        {
            // In real implementation, this would get device info from user service
            return $"- Device_{obCustId}";
        }       

        private async Task<ExchangeRate> GetExchangeRate()
        {
            // In real implementation, this would get exchange rates from a service
            return new ExchangeRate { Code = new Dictionary<string, decimal> { { "USD", 1.0m } } };
        }

        private async Task<List<Currency>> GetCurrencies()
        {
            // In real implementation, this would get currencies from a service
            return new List<Currency> 
            { 
                new Currency { Enabled = true, Name = "USD", Code = "USD", Multiplier = 1.0m, PartnerId = "1" }
            };
        }

        private async Task<User> GetUser(string obCustId)
        {
            // In real implementation, this would get user from a service
            return new User { OperatorId = "1" };
        }       
    }
}