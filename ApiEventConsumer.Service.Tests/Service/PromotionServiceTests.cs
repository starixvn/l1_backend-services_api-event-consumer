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
        private readonly IAppSettings _appSettings;
        private readonly ISentryClient _sentryClient;

        public PromotionServiceTests()
        {
            _appSettings = Substitute.For<IAppSettings>();
            _sentryClient = Substitute.For<ISentryClient>();

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
            var service = new PromotionService(httpClient, _appSettings, _sentryClient);

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
            var service = new PromotionService(httpClient, _appSettings, _sentryClient);

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
            var service = new PromotionService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Exception: Network error", result.ErrorMessage);
            Assert.Equal("EXCEPTION", result.ErrorCode);
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
                BetAmount = 100.50m
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "null");
            
            var httpClient = new HttpClient(mockHandler);
            var service = new PromotionService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.UpdateTotalTurnoverAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Exception: Failed to deserialize response", result.ErrorMessage);
            Assert.Equal("EXCEPTION", result.ErrorCode);
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