using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Service.Service;
using NATS.Client;

namespace ApiEventConsumer.Subscribers.User
{
    public class UserSubscriber : IEventSubscriber
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        
        public string SubjectName => "user.activity";

        public UserSubscriber(
            ISentryClient sentryClient,
            ConfigurationServices configurations)
        {
            this.sentryClient = sentryClient;
            this.appSettings = configurations.AppSettings;
        }

        public void OnMessageReceived(object sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var userActivity = JsonSerializer.Deserialize<UserActivity>(message);
                
                // Process user activity
                // Example: Log user activity, update user statistics, etc.
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(ex);
            }
        }
    }

    // Example data model
    public class UserActivity
    {
        public string? UserId { get; set; }
        public string? ActivityType { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }
} 