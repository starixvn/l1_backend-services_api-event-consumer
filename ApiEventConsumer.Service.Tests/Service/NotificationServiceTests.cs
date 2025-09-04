using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Services;
using NSubstitute;
using Sentry;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class NotificationServiceTests
    {
        private static IAppSettings CreateMockAppSettings()
        {
            var appSettings = Substitute.For<IAppSettings>();
            var notificationApiSettings = new NotificationApiSettings
            {
                BaseUrl = "https://test-notification-api.com",
                PromotionProgressEndpoint = "/api/v1/promotionProgress",
                TimeoutInSeconds = 30
            };
            appSettings.NotificationApiSettings = notificationApiSettings;
            return appSettings;
        }

        private static PromotionProgressRequest CreateValidRequest()
        {
            return new PromotionProgressRequest
            {
                PlayerId = "player123",
                OperatorId = "op001",
                SiteName = "testsite",
                TotalTO = 1000.50m,
                MinTO = 100.00m,
                RemainTO = 200.25m,
                Progress = 75.5m,
                CanClaim = true,
                Win = 50.00m,
                Expire = DateTime.UtcNow.AddDays(30)
            };
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var expectedResponse = new PromotionProgressResponse
            {
                Success = true,
                Code = "SUCCESS",
                Message = "Promotion progress updated successfully"
            };

            var expectedUrl = new Uri("https://test-notification-api.com/api/v1/promotionProgress");
            baseApiService
                .PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                    expectedUrl,
                    request,
                    30)
                .Returns(Task.FromResult(expectedResponse));

            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("SUCCESS", result.Code);
            Assert.Equal("Promotion progress updated successfully", result.Message);

            await baseApiService.Received(1).PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                expectedUrl,
                request,
                30);
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithDefaultEndpoint_ShouldUseDefaultPath()
        {
            // Arrange
            var appSettings = Substitute.For<IAppSettings>();
            var notificationApiSettings = new NotificationApiSettings
            {
                BaseUrl = "https://test-notification-api.com",
                PromotionProgressEndpoint = null, // This should trigger default endpoint
                TimeoutInSeconds = 30
            };
            appSettings.NotificationApiSettings = notificationApiSettings;

            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var expectedResponse = new PromotionProgressResponse
            {
                Success = true,
                Code = "SUCCESS",
                Message = "Success"
            };

            var expectedUrl = new Uri("https://test-notification-api.com/api/promotionProgress");
            baseApiService
                .PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                    expectedUrl,
                    request,
                    30)
                .Returns(Task.FromResult(expectedResponse));

            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);

            await baseApiService.Received(1).PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                expectedUrl,
                request,
                30);
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithNullNotificationApiSettings_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var appSettings = Substitute.For<IAppSettings>();
            appSettings.NotificationApiSettings = null;

            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("EXCEPTION", result.Code);
            Assert.Contains("NotificationApiSettings or BaseUrl is not configured", result.Message);

            // Verify Sentry was called with an exception
            sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);

            // Verify base API service was not called
            await baseApiService.DidNotReceive().PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                Arg.Any<Uri>(),
                Arg.Any<PromotionProgressRequest>(),
                Arg.Any<int?>());
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithEmptyBaseUrl_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var appSettings = Substitute.For<IAppSettings>();
            var notificationApiSettings = new NotificationApiSettings
            {
                BaseUrl = string.Empty,
                PromotionProgressEndpoint = "/api/v1/promotionProgress",
                TimeoutInSeconds = 30
            };
            appSettings.NotificationApiSettings = notificationApiSettings;

            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("EXCEPTION", result.Code);
            Assert.Contains("NotificationApiSettings or BaseUrl is not configured", result.Message);

            // Verify Sentry was called with an exception
            sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithNullBaseUrl_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var appSettings = Substitute.For<IAppSettings>();
            var notificationApiSettings = new NotificationApiSettings
            {
                BaseUrl = null,
                PromotionProgressEndpoint = "/api/v1/promotionProgress",
                TimeoutInSeconds = 30
            };
            appSettings.NotificationApiSettings = notificationApiSettings;

            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("EXCEPTION", result.Code);
            Assert.Contains("NotificationApiSettings or BaseUrl is not configured", result.Message);

            // Verify Sentry was called with an exception
            sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithWhitespaceBaseUrl_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var appSettings = Substitute.For<IAppSettings>();
            var notificationApiSettings = new NotificationApiSettings
            {
                BaseUrl = "   ",
                PromotionProgressEndpoint = "/api/v1/promotionProgress",
                TimeoutInSeconds = 30
            };
            appSettings.NotificationApiSettings = notificationApiSettings;

            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("EXCEPTION", result.Code);
            Assert.Contains("NotificationApiSettings or BaseUrl is not configured", result.Message);

            // Verify Sentry was called with an exception
            sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithBaseApiServiceException_ShouldCaptureExceptionAndReturnErrorResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var exception = new HttpRequestException("Network timeout occurred");

            var expectedUrl = new Uri("https://test-notification-api.com/api/v1/promotionProgress");
            baseApiService
                .PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                    expectedUrl,
                    request,
                    30)
                .Returns(Task.FromException<PromotionProgressResponse>(exception));

            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("EXCEPTION", result.Code);
            Assert.Contains("Exception: Network timeout occurred", result.Message);

            // Verify Sentry was called with the exception
            sentryClient.Received(1).CaptureException(exception);
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithGenericException_ShouldCaptureExceptionAndReturnErrorResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var exception = new InvalidOperationException("Some unexpected error");

            var expectedUrl = new Uri("https://test-notification-api.com/api/v1/promotionProgress");
            baseApiService
                .PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                    expectedUrl,
                    request,
                    30)
                .Returns(Task.FromException<PromotionProgressResponse>(exception));

            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("EXCEPTION", result.Code);
            Assert.Contains("Exception: Some unexpected error", result.Message);

            // Verify Sentry was called with the exception
            sentryClient.Received(1).CaptureException(exception);
        }

        [Fact]
        public async Task SendPromotionProgressAsync_WithSuccessfulButFailedResponse_ShouldReturnFailedResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var baseApiService = Substitute.For<IBaseApiService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var request = CreateValidRequest();
            var failedResponse = new PromotionProgressResponse
            {
                Success = false,
                Code = "VALIDATION_ERROR",
                Message = "Invalid player data"
            };

            var expectedUrl = new Uri("https://test-notification-api.com/api/v1/promotionProgress");
            baseApiService
                .PostAsync<PromotionProgressRequest, PromotionProgressResponse>(
                    expectedUrl,
                    request,
                    30)
                .Returns(Task.FromResult(failedResponse));

            var service = new NotificationService(baseApiService, appSettings, sentryClient);

            // Act
            var result = await service.SendPromotionProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("VALIDATION_ERROR", result.Code);
            Assert.Equal("Invalid player data", result.Message);

            // Verify no exception was captured
            sentryClient.DidNotReceiveWithAnyArgs().CaptureException(default!);
        }
    }
}
