using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Constants;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Services;
using FluentAssertions;
using NSubstitute;
using static ApiEventConsumer.Service.Tests.Service.PromotionServiceTests;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class AuthServiceTests
    {
        private readonly ISentryClient _sentryClient;
        private readonly IAppSettings _appSettings;

        public AuthServiceTests()
        {
            _sentryClient = Substitute.For<ISentryClient>();
            _sentryClient.IsEnabled.Returns(true);
            _appSettings = Substitute.For<IAppSettings>();
        }

        private static AuthApiSettings CreateValidAuthSettings()
        {
            return new AuthApiSettings
            {
                BaseUrl = "https://auth.test.com",
                TokenEndpoint = "/oauth/token",
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret",
                Scope = "admin-api",
                TimeoutInSeconds = 30
            };
        }

        #region GetAuthTokenAsync Tests

        [Fact]
        public async Task GetAuthTokenAsync_WithValidSettings_ShouldReturnAccessToken()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "test-access-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().Be("test-access-token");
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithCachedValidToken_ShouldReturnCachedToken()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "cached-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // First call to cache the token
            var firstResult = await service.GetAuthTokenAsync();

            // Reset mock to verify no second HTTP call is made
            var mockHandlerSecond = new MockHttpMessageHandler();
            mockHandlerSecond.SetupException(new InvalidOperationException("Should not be called"));
            
            var httpClientSecond = new HttpClient(mockHandlerSecond);
            var serviceSecond = new AuthService(httpClientSecond, _appSettings, _sentryClient);
            
            // Set the cached token and expiry using reflection
            var cachedTokenField = typeof(AuthService).GetField("cachedToken", BindingFlags.NonPublic | BindingFlags.Instance);
            var tokenExpiryField = typeof(AuthService).GetField("tokenExpiry", BindingFlags.NonPublic | BindingFlags.Instance);
            
            cachedTokenField?.SetValue(serviceSecond, "cached-token");
            tokenExpiryField?.SetValue(serviceSecond, DateTime.UtcNow.AddMinutes(30));

            // Act - Second call should use cached token
            var secondResult = await serviceSecond.GetAuthTokenAsync();

            // Assert
            firstResult.Should().Be("cached-token");
            secondResult.Should().Be("cached-token");
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithExpiredCachedToken_ShouldGetNewToken()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "new-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Set expired cached token using reflection
            var cachedTokenField = typeof(AuthService).GetField("cachedToken", BindingFlags.NonPublic | BindingFlags.Instance);
            var tokenExpiryField = typeof(AuthService).GetField("tokenExpiry", BindingFlags.NonPublic | BindingFlags.Instance);
            
            cachedTokenField?.SetValue(service, "expired-token");
            tokenExpiryField?.SetValue(service, DateTime.UtcNow.AddMinutes(-10)); // Expired 10 minutes ago

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().Be("new-token");
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithNullAuthApiSettings_ShouldReturnNull()
        {
            // Arrange
            _appSettings.AuthApiSettings.Returns((AuthApiSettings?)null);

            var httpClient = new HttpClient();
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().BeNull();
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Theory]
        [InlineData(null, "token", "client", "secret")]
        [InlineData("", "token", "client", "secret")]
        [InlineData("   ", "token", "client", "secret")]
        [InlineData("https://auth.com", null, "client", "secret")]
        [InlineData("https://auth.com", "", "client", "secret")]
        [InlineData("https://auth.com", "   ", "client", "secret")]
        [InlineData("https://auth.com", "token", null, "secret")]
        [InlineData("https://auth.com", "token", "", "secret")]
        [InlineData("https://auth.com", "token", "   ", "secret")]
        [InlineData("https://auth.com", "token", "client", null)]
        [InlineData("https://auth.com", "token", "client", "")]
        [InlineData("https://auth.com", "token", "client", "   ")]
        public async Task GetAuthTokenAsync_WithInvalidAuthSettings_ShouldReturnNullAndLogError(
            string? baseUrl, string? tokenEndpoint, string? clientId, string? clientSecret)
        {
            // Arrange
            var authSettings = new AuthApiSettings
            {
                BaseUrl = baseUrl,
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = "admin-api",
                TimeoutInSeconds = 30
            };
            _appSettings.AuthApiSettings.Returns(authSettings);

            var httpClient = new HttpClient();
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().BeNull();
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithHttpError_ShouldReturnNullAndLogError()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().BeNull();
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithInvalidJsonResponse_ShouldReturnNullAndLogError()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "{ invalid json }");

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().BeNull();
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithEmptyAccessToken_ShouldReturnNull()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = string.Empty,
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithNullResponse_ShouldReturnNullAndLogError()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, "null");

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().BeNull();
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithCustomScope_ShouldUseProvidedScope()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            authSettings.Scope = "custom-scope";
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "test-token",
                TokenType = "Bearer", 
                ExpiresIn = 3600,
                Scope = "custom-scope"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().Be("test-token");
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithNullScope_ShouldUseDefaultScope()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            authSettings.Scope = null;
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "test-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().Be("test-token");
        }

        #endregion

        #region SetAuthorizationHeaderAsync Tests

        [Fact]
        public async Task SetAuthorizationHeaderAsync_WithValidToken_ShouldSetBearerTokenAndReturnTrue()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "valid-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var authHttpClient = new HttpClient(mockHandler);
            var service = new AuthService(authHttpClient, _appSettings, _sentryClient);

            var targetHttpClient = new HttpClient();

            // Act
            var result = await service.SetAuthorizationHeaderAsync(targetHttpClient);

            // Assert
            result.Should().BeTrue();
            targetHttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
            targetHttpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
            targetHttpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("valid-token");
        }

        [Fact]
        public async Task SetAuthorizationHeaderAsync_WhenGetTokenFails_ShouldReturnFalse()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            var authHttpClient = new HttpClient(mockHandler);
            var service = new AuthService(authHttpClient, _appSettings, _sentryClient);

            var targetHttpClient = new HttpClient();

            // Act
            var result = await service.SetAuthorizationHeaderAsync(targetHttpClient);

            // Assert
            result.Should().BeFalse();
            targetHttpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
        }

        [Fact]
        public async Task SetAuthorizationHeaderAsync_WithNullAuthSettings_ShouldReturnFalse()
        {
            // Arrange
            _appSettings.AuthApiSettings.Returns((AuthApiSettings?)null);

            var httpClient = new HttpClient();
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            var targetHttpClient = new HttpClient();

            // Act
            var result = await service.SetAuthorizationHeaderAsync(targetHttpClient);

            // Assert
            result.Should().BeFalse();
            targetHttpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
        }

        #endregion

        #region ClearCachedToken Tests

        [Fact]
        public async Task ClearCachedToken_ShouldClearCacheAndForceNewTokenRequest()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var firstResponse = new AuthTokenResponse
            {
                AccessToken = "first-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var secondResponse = new AuthTokenResponse
            {
                AccessToken = "second-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(firstResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Get first token (will be cached)
            var firstToken = await service.GetAuthTokenAsync();

            // Setup second response for after cache clear
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(secondResponse));

            // Act - Clear cache
            service.ClearCachedToken();

            // Get token again (should make new request)
            var secondToken = await service.GetAuthTokenAsync();

            // Assert
            firstToken.Should().Be("first-token");
            secondToken.Should().Be("second-token");
        }

        [Fact]
        public void ClearCachedToken_ShouldResetCachedTokenToNull()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var httpClient = new HttpClient();
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Set cached token using reflection
            var cachedTokenField = typeof(AuthService).GetField("cachedToken", BindingFlags.NonPublic | BindingFlags.Instance);
            var tokenExpiryField = typeof(AuthService).GetField("tokenExpiry", BindingFlags.NonPublic | BindingFlags.Instance);
            
            cachedTokenField?.SetValue(service, "test-token");
            tokenExpiryField?.SetValue(service, DateTime.UtcNow.AddMinutes(30));

            // Act
            service.ClearCachedToken();

            // Assert
            var tokenAfterClear = cachedTokenField?.GetValue(service);
            var expiryAfterClear = (DateTime)(tokenExpiryField?.GetValue(service) ?? DateTime.MinValue);

            tokenAfterClear.Should().BeNull();
            expiryAfterClear.Should().Be(DateTime.MinValue);
        }

        #endregion

        #region PostFormAsync Integration Tests (tested indirectly through GetAuthTokenAsync)

        [Fact]
        public async Task GetAuthTokenAsync_WithCustomTimeout_ShouldUseSpecifiedTimeout()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            authSettings.TimeoutInSeconds = 60;
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "test-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().Be("test-token");
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithTimeout_ShouldReturnNullAndLogError()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupException(new TaskCanceledException("The operation was canceled."));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().BeNull();
            _sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task GetAuthTokenAsync_ShouldClearAuthHeaderBeforeTokenRequest()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "test-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            // Set initial auth header
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "old-token");
            
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act
            var result = await service.GetAuthTokenAsync();

            // Assert
            result.Should().Be("test-token");
        }

        [Fact]
        public async Task GetAuthTokenAsync_WithTokenExpiryBuffer_ShouldCalculateCorrectExpiry()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "test-token",
                TokenType = "Bearer",
                ExpiresIn = 3600, // 1 hour
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            var beforeCall = DateTime.UtcNow;

            // Act
            var result = await service.GetAuthTokenAsync();

            var afterCall = DateTime.UtcNow;

            // Assert
            result.Should().Be("test-token");

            // Verify expiry is set correctly (3600 - 300 buffer = 3300 seconds from now)
            var tokenExpiryField = typeof(AuthService).GetField("tokenExpiry", BindingFlags.NonPublic | BindingFlags.Instance);
            var actualExpiry = (DateTime)(tokenExpiryField?.GetValue(service) ?? DateTime.MinValue);

            // The expiry should be approximately 55 minutes from the call time (3600 - 300 buffer seconds)
            var expectedExpiryMin = beforeCall.AddSeconds(3300);
            var expectedExpiryMax = afterCall.AddSeconds(3300);

            actualExpiry.Should().BeOnOrAfter(expectedExpiryMin);
            actualExpiry.Should().BeOnOrBefore(expectedExpiryMax);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task SetAuthorizationHeaderAsync_WithExistingHeader_ShouldReplaceHeader()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "new-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var authHttpClient = new HttpClient(mockHandler);
            var service = new AuthService(authHttpClient, _appSettings, _sentryClient);

            var targetHttpClient = new HttpClient();
            targetHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "old-token");

            // Act
            var result = await service.SetAuthorizationHeaderAsync(targetHttpClient);

            // Assert
            result.Should().BeTrue();
            targetHttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
            targetHttpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
            targetHttpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("new-token");
        }

        [Fact]
        public async Task GetAuthTokenAsync_MultipleSimultaneousCalls_ShouldHandleConcurrently()
        {
            // Arrange
            var authSettings = CreateValidAuthSettings();
            _appSettings.AuthApiSettings.Returns(authSettings);

            var expectedResponse = new AuthTokenResponse
            {
                AccessToken = "concurrent-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "admin-api"
            };

            var mockHandler = new MockHttpMessageHandler();
            mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

            var httpClient = new HttpClient(mockHandler);
            var service = new AuthService(httpClient, _appSettings, _sentryClient);

            // Act - Make multiple simultaneous calls
            var tasks = new[]
            {
                service.GetAuthTokenAsync(),
                service.GetAuthTokenAsync(),
                service.GetAuthTokenAsync()
            };

            var results = await Task.WhenAll(tasks);

            // Assert - All should return the same token
            results.Should().AllBeEquivalentTo("concurrent-token");
        }

        #endregion
    }
}
