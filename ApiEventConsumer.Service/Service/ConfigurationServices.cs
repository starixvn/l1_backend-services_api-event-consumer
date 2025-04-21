using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Subscribers;

namespace ApiEventConsumer.Service.Service
{
    public interface IConfigurationService
    {
    }

    public class ConfigurationServices : IConfigurationService
    {
        public IConfiguration Configuration { get; }
        public ICacheManager Cache { get; }
        public IAppSettings AppSettings { get; }
        public IGameUserService GameUserService { get; }
        public IGeneralGameSettingService GeneralGameSettingService { get; }
        public IChatBotApi ChatBotApi { get; }
        public ConfigurationServices(
            IConfiguration configuration,
            ICacheManager cache,
            IAppSettings appSettings, 
            IGameUserService gameUserService,
            IGeneralGameSettingService generalGameSettingService,
            IChatBotApi chatBotApi)
        {
            Configuration = configuration;
            Cache = cache;
            AppSettings = appSettings;
            GameUserService = gameUserService;
            GeneralGameSettingService = generalGameSettingService;
            ChatBotApi = chatBotApi;
        }
    }
}
