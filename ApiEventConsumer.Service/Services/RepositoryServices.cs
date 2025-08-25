using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;

namespace ApiEventConsumer.Services
{
    public class RepositoryServices
    {
        public IGenericRepository<UserSessionEntity> UserSessionRepository { get; }
        public IGenericRepository<ExchangeRateEntity> ExchangeRateRepository { get; }
        public IGenericRepository<CurrencyEntity> CurrencyRepository { get; }
        public IGenericRepository<AccountEntity> UserRepository { get; }

        public RepositoryServices(
            IGenericRepository<UserSessionEntity> userSessionRepository,
            IGenericRepository<ExchangeRateEntity> exchangeRateRepository,
            IGenericRepository<CurrencyEntity> currencyRepository,
            IGenericRepository<AccountEntity> userRepository)
        {
            UserSessionRepository = userSessionRepository;
            ExchangeRateRepository = exchangeRateRepository;
            CurrencyRepository = currencyRepository;
            UserRepository = userRepository;
        }
    }
}
