using Coravel.Events.Interfaces;
using Coravel.Invocable;
using ApiEventConsumer.Events;

namespace ApiEventConsumer.Providers
{
    public class HighStakeInvocable : IInvocable
    {
        private const string Source = "OW";
        private readonly IProviderService providerService;
        private readonly IDispatcher dispatcher;

        public HighStakeInvocable(IProviderService providerService, IDispatcher dispatcher)
        {
            this.providerService = providerService;
            this.dispatcher = dispatcher;
        }

        public async Task Invoke()
        {
            var ApiEventConsumers = await providerService.GetHighStake();           

            await dispatcher.Broadcast(new HighStakeReceived(Source, ApiEventConsumers));
        }
    }
}