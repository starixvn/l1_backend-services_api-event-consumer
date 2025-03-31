using Coravel.Events.Interfaces;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Events;
using ApiEventConsumer.Subscribers.SC;
using ApiEventConsumer.Subscribers.SN;
using ApiEventConsumer.Subscribers.VG;

namespace ApiEventConsumer.Subscribers
{
    public class HighStakeListener : IListener<HighStakeReceived>
    {
        private readonly ISCSubscriberService SCService;
        private readonly IVGSubscriberService VGService;
        private readonly ISNSubscriberService SNService;
        private readonly IAppSettings appSettings;

        public HighStakeListener(ISCSubscriberService scService, IVGSubscriberService vgService, ISNSubscriberService snService, IAppSettings appSettings)
        {
            SCService = scService;
            VGService = vgService;
            SNService = snService;
            this.appSettings = appSettings;
        }

        public async Task HandleAsync(HighStakeReceived broadcasted)
        {
            await SCService.InsertApiEventConsumers(broadcasted.Source, broadcasted.HighStake);
            await VGService.InsertApiEventConsumers(broadcasted.Source, broadcasted.HighStake);
            await SNService.InsertApiEventConsumers(broadcasted.Source, broadcasted.HighStake);
        }
    }
}