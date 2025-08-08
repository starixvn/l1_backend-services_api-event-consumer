using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Subscribers;

namespace ApiEventConsumer.Services
{
    public class ConfigurationServices(
        IConfiguration configuration,
        ICacheManager cache,
        IAppSettings appSettings,
        IGameUserService gameUserService,
        IGeneralGameSettingService generalGameSettingService,
        IChatBotApi chatBotApi)
    {
        public IConfiguration Configuration { get; } = configuration;
        public ICacheManager Cache { get; } = cache;
        public IAppSettings AppSettings { get; } = appSettings;
        public IGameUserService GameUserService { get; } = gameUserService;
        public IGeneralGameSettingService GeneralGameSettingService { get; } = generalGameSettingService;
        public IChatBotApi ChatBotApi { get; } = chatBotApi;
    }
}
