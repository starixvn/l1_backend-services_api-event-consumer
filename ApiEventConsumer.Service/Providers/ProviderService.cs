using System.Data;

namespace ApiEventConsumer.Providers
{
    public interface IProviderService
    {
        Task<IEnumerable<HighStakeRecord>> GetHighStake();
    }

    public class ProviderService : IProviderService
    {
        private readonly ProviderSettings settings;
        private readonly IDapperContext context;

        public ProviderService(ProviderSettings settings, IDapperContext context)
        {
            this.settings = settings;
            this.context = context;
        }

        public Task<IEnumerable<HighStakeRecord>> GetHighStake()
        => context.QueryAsync<HighStakeRecord>
                (settings.StoredProcedure, commandType: CommandType.StoredProcedure);
    }
}