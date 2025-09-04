using System.Net;
using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Services;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class PromotionServiceTests
    {
        private static IAppSettings CreateMockAppSettings()
        {
            var appSettings = Substitute.For<IAppSettings>();
            var promotionApiSettings = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                TimeoutInSeconds = 30,
                Endpoints = new PromotionEndpoints
                {
                    UpdateTotalTurnover = "/bonus-wins/update-total-turnover",
                    HasBonusWin = "/bonus-wins/has-bonus-win",
                    GetRunningPromotions = "/promotions/running"
                }
            };
            appSettings.PromotionApiSettings = promotionApiSettings;
            return appSettings;
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                BetAmount = 100.50m
            };

            var expectedResponse = new UpdateTotalTurnoverResponse
            {
                Success = true,
                TotalTurnover = 150.75m,
                RemainingTO = 849.25m,
                ErrorCode = "SUCCESS",
                ErrorMessage = "Success"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Success", result.ErrorMessage);
            Assert.Equal("SUCCESS", result.ErrorCode);
            Assert.Equal(150.75m, result.TotalTurnover);
            Assert.Equal(849.25m, result.RemainingTO);
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithHttpError_ShouldReturnErrorResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                BetAmount = 100.50m
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.BadRequest, "Bad Request");

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Response status code does not indicate success", result.ErrorMessage);
            Assert.Equal("EXCEPTION", result.ErrorCode);
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithException_ShouldCaptureExceptionAndReturnErrorResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                BetAmount = 100.50m
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupException(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Exception: Network error", result.ErrorMessage);
            Assert.Equal("EXCEPTION", result.ErrorCode);
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithEmptyResponse_ShouldReturnErrorResponse()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                BetAmount = 100.50m
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "null");

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Exception: Failed to deserialize response", result.ErrorMessage);
            Assert.Equal("EXCEPTION", result.ErrorCode);
        }

        #region GetRunningPromotionsAsync Tests

        [Fact]
        public async Task GetRunningPromotionsAsync_WithValidResponse_ShouldReturnPromotions()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var expectedPromotions = new List<Promotion>
            {
                new Promotion
                {
                    Name = "Test Promotion 1",
                    Code = "PROMO1",
                    OperatorId = "op1",
                    OfferedSiteNames = ["site1", "site2"],
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    ClaimExpirationDate = DateTime.UtcNow.AddDays(35),
                    GameIds = [1, 2, 3],
                    MinTurnoverMultiplier = 2.5m
                },
                new Promotion
                {
                    Name = "Test Promotion 2",
                    Code = "PROMO2",
                    OperatorId = "op2",
                    OfferedSiteNames = [],
                    StartDate = DateTime.UtcNow.AddDays(-5),
                    EndDate = DateTime.UtcNow.AddDays(15),
                    ClaimExpirationDate = DateTime.UtcNow.AddDays(20),
                    GameIds = [4, 5],
                    MinTurnoverMultiplier = 3.0m
                }
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedPromotions));

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            
            var firstPromotion = result.First();
            Assert.Equal("Test Promotion 1", firstPromotion.Name);
            Assert.Equal("PROMO1", firstPromotion.Code);
            Assert.Equal("op1", firstPromotion.OperatorId);
            Assert.Equal(2, firstPromotion.OfferedSiteNames.Count);
            Assert.Contains("site1", firstPromotion.OfferedSiteNames);
            Assert.Contains("site2", firstPromotion.OfferedSiteNames);
            Assert.Equal(3, firstPromotion.GameIds.Count);
            Assert.Equal(2.5m, firstPromotion.MinTurnoverMultiplier);

            var secondPromotion = result.Last();
            Assert.Equal("Test Promotion 2", secondPromotion.Name);
            Assert.Equal("PROMO2", secondPromotion.Code);
            Assert.Equal("op2", secondPromotion.OperatorId);
            Assert.Empty(secondPromotion.OfferedSiteNames);
            Assert.Equal(2, secondPromotion.GameIds.Count);
            Assert.Equal(3.0m, secondPromotion.MinTurnoverMultiplier);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithEmptyResponse_ShouldReturnEmptyList()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var emptyPromotions = new List<Promotion>();
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(emptyPromotions));

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithHttpError_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithNetworkException_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupException(new HttpRequestException("Network timeout"));

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithNullResponse_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "null");

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithInvalidJson_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var appSettings = CreateMockAppSettings();
            var sentryClient = Substitute.For<ISentryClient>();
            
            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "{ invalid json }");

            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, appSettings, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithNullPromotionApiSettings_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettingsWithNullPromotion = Substitute.For<IAppSettings>();
            appSettingsWithNullPromotion.PromotionApiSettings = null;

            var httpClient = new HttpClient();
            var service = new PromotionService(httpClient, appSettingsWithNullPromotion, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithEmptyBaseUrl_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var promotionApiSettingsWithEmptyBaseUrl = new PromotionApiSettings
            {
                BaseUrl = string.Empty,
                Endpoints = new PromotionEndpoints
                {
                    GetRunningPromotions = "/promotions/running"
                }
            };
            var appSettingsWithEmptyBaseUrl = Substitute.For<IAppSettings>();
            appSettingsWithEmptyBaseUrl.PromotionApiSettings = promotionApiSettingsWithEmptyBaseUrl;

            var httpClient = new HttpClient();
            var service = new PromotionService(httpClient, appSettingsWithEmptyBaseUrl, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithNullEndpoints_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var promotionApiSettingsWithNullEndpoints = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = null
            };
            var appSettingsWithNullEndpoints = Substitute.For<IAppSettings>();
            appSettingsWithNullEndpoints.PromotionApiSettings = promotionApiSettingsWithNullEndpoints;

            var httpClient = new HttpClient();
            var service = new PromotionService(httpClient, appSettingsWithNullEndpoints, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRunningPromotionsAsync_WithEmptyGetRunningPromotionsEndpoint_ShouldCaptureExceptionAndReturnEmptyList()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var promotionApiSettingsWithEmptyEndpoint = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = new PromotionEndpoints
                {
                    GetRunningPromotions = string.Empty
                }
            };
            var appSettingsWithEmptyEndpoint = Substitute.For<IAppSettings>();
            appSettingsWithEmptyEndpoint.PromotionApiSettings = promotionApiSettingsWithEmptyEndpoint;

            var httpClient = new HttpClient();
            var service = new PromotionService(httpClient, appSettingsWithEmptyEndpoint, sentryClient);

            // Act
            var result = await service.GetRunningPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion


    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _content = string.Empty;
        private Exception? _exception;

        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
            _exception = null;
        }

        public void SetupException(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}