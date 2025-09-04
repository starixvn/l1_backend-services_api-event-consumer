using System.Net;
using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Constants;
using ApiEventConsumer.Services;
using FluentAssertions;
using NSubstitute;
using static ApiEventConsumer.Service.Tests.Service.PromotionServiceTests;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class BaseApiServiceTests
    {
        private readonly ISentryClient _sentryClient;
        private readonly IAppSettings _appSettings;
        private readonly IAuthService _authService;

        public BaseApiServiceTests()
        {
            _sentryClient = Substitute.For<ISentryClient>();
            _sentryClient.IsEnabled.Returns(true);
            _appSettings = Substitute.For<IAppSettings>();
            _authService = Substitute.For<IAuthService>();
        }

        #region PostAsync Tests

        [Fact]
        public async Task PostAsync_WithValidRequestAndAuth_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var expectedResponse = new TestResponse { Success = true, Message = "OK" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act
            var result = await service.PostAsync<TestRequest, TestResponse>(url, request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("OK");
            await _authService.Received(1).SetAuthorizationHeaderAsync(httpClient);
        }

        [Fact]
        public async Task PostAsync_WithoutAuth_ShouldNotSetAuthorizationHeader()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var expectedResponse = new TestResponse { Success = true, Message = "OK" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            // Act
            var result = await service.PostAsync<TestRequest, TestResponse>(url, request, requireAuth: false);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            await _authService.DidNotReceiveWithAnyArgs().SetAuthorizationHeaderAsync(default!);
        }

        [Fact]
        public async Task PostAsync_WhenAuthFails_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.PostAsync<TestRequest, TestResponse>(url, request));

            exception.Message.Should().Be(ApiServiceConstants.FailedToSetAuthorizationHeader);
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task PostAsync_WithHttpError_ShouldThrowHttpRequestExceptionAndLogToSentry()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.BadRequest, "Bad Request");

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                async () => await service.PostAsync<TestRequest, TestResponse>(url, request));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task PostAsync_WithCustomTimeout_ShouldUseSpecifiedTimeout()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var expectedResponse = new TestResponse { Success = true, Message = "OK" };
            var url = new Uri("https://api.test.com/endpoint");
            const int customTimeout = 60;

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act
            var result = await service.PostAsync<TestRequest, TestResponse>(url, request, customTimeout);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task PostAsync_WithNullResponse_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "null");

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.PostAsync<TestRequest, TestResponse>(url, request));

            exception.Message.Should().Be(string.Format(ApiServiceConstants.FailedToDeserializeResponse, url));
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        #endregion

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_WithValidRequestAndAuth_ShouldReturnSuccessResponse()
        {
            // Arrange
            var expectedResponse = new TestResponse { Success = true, Message = "OK" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act
            var result = await service.GetAsync<TestResponse>(url);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("OK");
            await _authService.Received(1).SetAuthorizationHeaderAsync(httpClient);
        }

        [Fact]
        public async Task GetAsync_WithoutAuth_ShouldNotSetAuthorizationHeader()
        {
            // Arrange
            var expectedResponse = new TestResponse { Success = true, Message = "OK" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            // Act
            var result = await service.GetAsync<TestResponse>(url, requireAuth: false);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            await _authService.DidNotReceiveWithAnyArgs().SetAuthorizationHeaderAsync(default!);
        }

        [Fact]
        public async Task GetAsync_WhenAuthFails_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.GetAsync<TestResponse>(url));

            exception.Message.Should().Be(ApiServiceConstants.FailedToSetAuthorizationHeader);
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAsync_WithHttpError_ShouldThrowHttpRequestExceptionAndLogToSentry()
        {
            // Arrange
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.NotFound, "Not Found");

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                async () => await service.GetAsync<TestResponse>(url));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        #endregion

        #region PutAsync Tests

        [Fact]
        public async Task PutAsync_WithValidRequestAndAuth_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new TestRequest { Name = "updated", Value = 456 };
            var expectedResponse = new TestResponse { Success = true, Message = "Updated" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act
            var result = await service.PutAsync<TestRequest, TestResponse>(url, request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Updated");
            await _authService.Received(1).SetAuthorizationHeaderAsync(httpClient);
        }

        [Fact]
        public async Task PutAsync_WithoutAuth_ShouldNotSetAuthorizationHeader()
        {
            // Arrange
            var request = new TestRequest { Name = "updated", Value = 456 };
            var expectedResponse = new TestResponse { Success = true, Message = "Updated" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            // Act
            var result = await service.PutAsync<TestRequest, TestResponse>(url, request, requireAuth: false);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            await _authService.DidNotReceiveWithAnyArgs().SetAuthorizationHeaderAsync(default!);
        }

        [Fact]
        public async Task PutAsync_WhenAuthFails_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var request = new TestRequest { Name = "updated", Value = 456 };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.PutAsync<TestRequest, TestResponse>(url, request));

            exception.Message.Should().Be(ApiServiceConstants.FailedToSetAuthorizationHeader);
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task PutAsync_WithHttpError_ShouldThrowHttpRequestExceptionAndLogToSentry()
        {
            // Arrange
            var request = new TestRequest { Name = "updated", Value = 456 };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.Conflict, "Conflict");

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                async () => await service.PutAsync<TestRequest, TestResponse>(url, request));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidRequestAndAuth_ShouldReturnSuccessResponse()
        {
            // Arrange
            var expectedResponse = new TestResponse { Success = true, Message = "Deleted" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act
            var result = await service.DeleteAsync<TestResponse>(url);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Deleted");
            await _authService.Received(1).SetAuthorizationHeaderAsync(httpClient);
        }

        [Fact]
        public async Task DeleteAsync_WithoutAuth_ShouldNotSetAuthorizationHeader()
        {
            // Arrange
            var expectedResponse = new TestResponse { Success = true, Message = "Deleted" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            // Act
            var result = await service.DeleteAsync<TestResponse>(url, requireAuth: false);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            await _authService.DidNotReceiveWithAnyArgs().SetAuthorizationHeaderAsync(default!);
        }

        [Fact]
        public async Task DeleteAsync_WhenAuthFails_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.DeleteAsync<TestResponse>(url));

            exception.Message.Should().Be(ApiServiceConstants.FailedToSetAuthorizationHeader);
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task DeleteAsync_WithHttpError_ShouldThrowHttpRequestExceptionAndLogToSentry()
        {
            // Arrange
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.Forbidden, "Forbidden");

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                async () => await service.DeleteAsync<TestResponse>(url));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        #endregion

        #region Timeout Tests

        [Fact]
        public async Task PostAsync_WithTimeout_ShouldThrowOperationCanceledExceptionAndLogToSentry()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupException(new TaskCanceledException("The operation was canceled."));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await service.PostAsync<TestRequest, TestResponse>(url, request, 1));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAsync_WithTimeout_ShouldThrowOperationCanceledExceptionAndLogToSentry()
        {
            // Arrange
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupException(new TaskCanceledException("The operation was canceled."));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await service.GetAsync<TestResponse>(url, 1));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        #endregion

        #region Deserialization Error Tests

        [Fact]
        public async Task PostAsync_WithInvalidJsonResponse_ShouldThrowJsonExceptionAndLogToSentry()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "{ invalid json }");

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<JsonException>(
                async () => await service.PostAsync<TestRequest, TestResponse>(url, request));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAsync_WithInvalidJsonResponse_ShouldThrowJsonExceptionAndLogToSentry()
        {
            // Arrange
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "{ invalid json }");

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<JsonException>(
                async () => await service.GetAsync<TestResponse>(url));

            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        #endregion

        #region Default Timeout Tests

        [Fact]
        public async Task PostAsync_WithoutSpecifiedTimeout_ShouldUseDefaultTimeout()
        {
            // Arrange
            var request = new TestRequest { Name = "test", Value = 123 };
            var expectedResponse = new TestResponse { Success = true, Message = "OK" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act
            var result = await service.PostAsync<TestRequest, TestResponse>(url, request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetAsync_WithoutSpecifiedTimeout_ShouldUseDefaultTimeout()
        {
            // Arrange
            var expectedResponse = new TestResponse { Success = true, Message = "OK" };
            var url = new Uri("https://api.test.com/endpoint");

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new BaseApiService(httpClient, _appSettings, _sentryClient, _authService);

            _authService.SetAuthorizationHeaderAsync(httpClient).Returns(true);

            // Act
            var result = await service.GetAsync<TestResponse>(url);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        #endregion

        #region Helper Classes

        public class TestRequest
        {
            public string? Name { get; set; }
            public int Value { get; set; }
        }

        public class TestResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
        }

        #endregion
    }
}
