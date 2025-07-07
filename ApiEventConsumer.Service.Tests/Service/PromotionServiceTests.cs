using System.Net;
using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Subscribers;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class PromotionServiceTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConfigurationServices _configurationServices;
        private readonly IAppSettings _appSettings;
        private readonly ISentryClient _sentryClient;
        private readonly HttpClient _httpClient;
        private readonly PromotionService _sut;

        public PromotionServiceTests()
        {
            _httpClientFactory = Substitute.For<IHttpClientFactory>();
            _appSettings = Substitute.For<IAppSettings>();
            _sentryClient = Substitute.For<ISentryClient>();
            _httpClient = new HttpClient(new MockHttpMessageHandler());

            // Setup the new PromotionApiSettings structure
            var promotionApiSettings = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = new PromotionEndpoints
                {
                    UpdateTotalTurnover = "/bonus-wins/update-total-turnover",
                    HasBonusWin = "/bonus-wins/has-bonus-win"
                }
            };
            _appSettings.PromotionApiSettings.Returns(promotionApiSettings);

            // Create a real ConfigurationServices with mocked dependencies
            var configuration = Substitute.For<IConfiguration>();
            var cache = Substitute.For<ICacheManager>();
            var gameUserService = Substitute.For<IGameUserService>();
            var generalGameSettingService = Substitute.For<IGeneralGameSettingService>();
            var chatBotApi = Substitute.For<IChatBotApi>();

            _configurationServices = new ConfigurationServices(
                configuration,
                cache,
                _appSettings,
                gameUserService,
                generalGameSettingService,
                chatBotApi);

            _httpClientFactory.CreateClient().Returns(_httpClient);

            _sut = new PromotionService(_httpClientFactory, _configurationServices, _sentryClient);
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                PromotionId = "promo1",
                BetAmount = 100.50m
            };

            var expectedResponse = new UpdateTotalTurnoverResponse
            {
                Success = true,
                TotalTurnover = 150.75m,
                RemainingTO = 849.25m,
                Code = "SUCCESS",
                Message = "Success"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));
            
            var httpClient = new HttpClient(mockHandler);
            _httpClientFactory.CreateClient().Returns(httpClient);

            // Setup the same PromotionApiSettings for this test
            var promotionApiSettings = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = new PromotionEndpoints
                {
                    UpdateTotalTurnover = "/bonus-wins/update-total-turnover",
                    HasBonusWin = "/bonus-wins/has-bonus-win"
                }
            };
            _appSettings.PromotionApiSettings.Returns(promotionApiSettings);

            var service = new PromotionService(_httpClientFactory, _configurationServices, _sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Success", result.Message);
            Assert.Equal("SUCCESS", result.Code);
            Assert.Equal(150.75m, result.TotalTurnover);
            Assert.Equal(849.25m, result.RemainingTO);
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithHttpError_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                PromotionId = "promo1",
                BetAmount = 100.50m
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.BadRequest, "Bad Request");
            
            var httpClient = new HttpClient(mockHandler);
            _httpClientFactory.CreateClient().Returns(httpClient);

            // Setup the same PromotionApiSettings for this test
            var promotionApiSettings = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = new PromotionEndpoints
                {
                    UpdateTotalTurnover = "/bonus-wins/update-total-turnover",
                    HasBonusWin = "/bonus-wins/has-bonus-win"
                }
            };
            _appSettings.PromotionApiSettings.Returns(promotionApiSettings);

            var service = new PromotionService(_httpClientFactory, _configurationServices, _sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("HTTP BadRequest", result.Message);
            Assert.Equal("EXCEPTION", result.Code);
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithException_ShouldCaptureExceptionAndReturnErrorResponse()
        {
            // Arrange
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                PromotionId = "promo1",
                BetAmount = 100.50m
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupException(new HttpRequestException("Network error"));
            
            var httpClient = new HttpClient(mockHandler);
            _httpClientFactory.CreateClient().Returns(httpClient);

            // Setup the same PromotionApiSettings for this test
            var promotionApiSettings = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = new PromotionEndpoints
                {
                    UpdateTotalTurnover = "/bonus-wins/update-total-turnover",
                    HasBonusWin = "/bonus-wins/has-bonus-win"
                }
            };
            _appSettings.PromotionApiSettings.Returns(promotionApiSettings);

            var service = new PromotionService(_httpClientFactory, _configurationServices, _sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Exception: Network error", result.Message);
            Assert.Equal("EXCEPTION", result.Code);
            _sentryClient.Received(1).CaptureException(Arg.Any<Exception>());
        }

        [Fact]
        public async Task UpdateTotalTurnoverAsync_WithEmptyResponse_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new UpdateTotalTurnoverRequest
            {
                PlayerId = "123",
                OperatorId = "op1",
                SiteName = "site1",
                PromotionId = "promo1",
                BetAmount = 100.50m
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "null");
            
            var httpClient = new HttpClient(mockHandler);
            _httpClientFactory.CreateClient().Returns(httpClient);

            // Setup the same PromotionApiSettings for this test
            var promotionApiSettings = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = new PromotionEndpoints
                {
                    UpdateTotalTurnover = "/bonus-wins/update-total-turnover",
                    HasBonusWin = "/bonus-wins/has-bonus-win"
                }
            };
            _appSettings.PromotionApiSettings.Returns(promotionApiSettings);

            var service = new PromotionService(_httpClientFactory, _configurationServices, _sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Empty response", result.Message);
            Assert.Equal("ERROR", result.Code);
        }

        private void Dispose()
        {
            _httpClient?.Dispose();
        }
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