using System.Data;

namespace ApiEventConsumer.Providers
{
    public interface IProviderService
    {
        Task<IEnumerable<HighStakeRecord>> GetApiEventConsumer();
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

        public Task<IEnumerable<HighStakeRecord>> GetApiEventConsumer()
        => context.QueryAsync<HighStakeRecord>
                (settings.HighStakeStoredProcedure, commandType: CommandType.StoredProcedure);
    }
}