using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using MongoDB.Driver;

namespace ApiEventConsumer.Service.Service
{
    public interface IGameUserService
    {
        Task<string> GetCharacterNameAsync(string memberId);
    }

    public class GameUserService : IGameUserService
    {

        private readonly IGenericRepository<AccountInfoEntity> accountInfoRepository;
        private readonly IGenericRepository<AccountEntity> accountRepository;

        public GameUserService(
            IGenericRepository<AccountInfoEntity> accountInfoRepository,
            IGenericRepository<AccountEntity> accountRepository)
        {
            this.accountInfoRepository = accountInfoRepository;
            this.accountRepository = accountRepository;
        }


        public async Task<string> GetCharacterNameAsync(string memberId)
        {
            var projectionAccount = Builders<AccountEntity>.Projection
                 .Include(x => x.Id)
                 .Include(x => x.Local!.MemberId);

            var account = await accountRepository.Collection()
                .Find(user => user.Local!.MemberId == memberId)
                .Project<AccountEntity>(projectionAccount)
                .SortByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (account != null)
            {
                var projectionAccountInfo = Builders<AccountInfoEntity>.Projection
                    .Include(x => x.UserId)
                    .Include(x => x.Name);

                var accountInfo = await accountInfoRepository.Collection()
                    .Find(x => x.UserId == account.Id.ToString())
                    .Project<AccountInfoEntity>(projectionAccountInfo)
                    .FirstOrDefaultAsync();

                return accountInfo?.Name ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
