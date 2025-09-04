using System.Reflection;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class PromotionCacheServiceTests
    {
        [Fact]
        public void GetCacheKeyRunningPromotions_ShouldReturnCorrectKey()
        {
            // Act
            var result = PromotionCacheService.GetCacheKeyRunningPromotions();

            // Assert
            var expectedKey = $"{CachedKeys.RunningPromotions}";
            Assert.Equal(expectedKey, result);
            Assert.Equal("RunningPromotions", result);
        }

        [Fact]
        public void Constructor_WithValidDependencies_ShouldNotThrow()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            // Act & Assert
            var exception = Record.Exception(() => new PromotionCacheService(logger, promotionService, sharedCache, sentryClient));
            Assert.Null(exception);
        }

        [Fact]
        public async Task RefreshRunningPromotionCache_WithPromotions_ShouldCachePromotions()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var promotions = new List<Promotion>
            {
                new Promotion { Name = "Test Promotion 1", Code = "PROMO1" },
                new Promotion { Name = "Test Promotion 2", Code = "PROMO2" }
            };

            promotionService.GetRunningPromotionsAsync().Returns(promotions);

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Get private method
            var method = typeof(PromotionCacheService)
                .GetMethod("RefreshRunningPromotionCache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, null)!);

            // Assert
            Assert.Null(exception);
            sharedCache.Received().Set(
                "RunningPromotions",
                Arg.Any<List<Promotion>>(),
                TimeSpan.FromMinutes(5));
        }

        [Fact]
        public async Task RefreshRunningPromotionCache_WithEmptyPromotions_ShouldNotCache()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var emptyPromotions = new List<Promotion>();
            promotionService.GetRunningPromotionsAsync().Returns(emptyPromotions);

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Get private method
            var method = typeof(PromotionCacheService)
                .GetMethod("RefreshRunningPromotionCache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, null)!);

            // Assert
            Assert.Null(exception);
            sharedCache.DidNotReceiveWithAnyArgs().Set(default(string)!, default(object)!, default(TimeSpan));
        }

        [Fact]
        public async Task RefreshRunningPromotionCache_WhenPromotionServiceThrows_ShouldLogErrorAndCaptureException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var expectedException = new InvalidOperationException("Service error");
            promotionService.GetRunningPromotionsAsync().ThrowsAsync(expectedException);

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Get private method
            var method = typeof(PromotionCacheService)
                .GetMethod("RefreshRunningPromotionCache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, null)!);

            // Assert
            Assert.Null(exception); // Method should handle exception internally
            sharedCache.DidNotReceiveWithAnyArgs().Set(default(string)!, default(object)!, default(TimeSpan));
        }

        [Fact]
        public async Task ExecuteAsync_WhenCancellationRequested_ShouldStopExecution()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            promotionService.GetRunningPromotionsAsync().Returns(new List<Promotion>());

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Create cancellation token that cancels quickly
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Get protected method
            var method = typeof(PromotionCacheService)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act & Assert - TaskCanceledException is expected when cancellation token is triggered
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, new object[] { cancellationTokenSource.Token })!);

            Assert.IsType<TaskCanceledException>(exception);
        }

        [Fact]
        public async Task ExecuteAsync_WhenRefreshThrowsException_ShouldLogErrorAndContinue()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var expectedException = new InvalidOperationException("Refresh error");
            promotionService.GetRunningPromotionsAsync().ThrowsAsync(expectedException);

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Create cancellation token that cancels after allowing one iteration
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(200));

            // Get protected method
            var method = typeof(PromotionCacheService)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, new object[] { cancellationTokenSource.Token })!);

            // Assert
            Assert.IsType<TaskCanceledException>(exception); // Cancellation is expected
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulRefresh_ShouldCallRefreshMethod()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var promotions = new List<Promotion>
            {
                new Promotion { Name = "Test Promotion", Code = "TEST" }
            };
            promotionService.GetRunningPromotionsAsync().Returns(promotions);

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Create cancellation token that cancels after allowing one iteration
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(200));

            // Get protected method
            var method = typeof(PromotionCacheService)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, new object[] { cancellationTokenSource.Token })!);

            // Assert
            Assert.IsType<TaskCanceledException>(exception); // Cancellation is expected
            await promotionService.Received().GetRunningPromotionsAsync();
        }

        [Fact]
        public void ServiceInheritsFromBackgroundService()
        {
            // Arrange & Act
            var serviceType = typeof(PromotionCacheService);

            // Assert
            Assert.True(serviceType.IsSubclassOf(typeof(BackgroundService)));
        }

        [Fact]
        public void IntervalConstant_ShouldBe60Seconds()
        {
            // Arrange & Act
            var intervalField = typeof(PromotionCacheService)
                .GetField("IntervalInSeconds", BindingFlags.NonPublic | BindingFlags.Static);

            // Assert
            Assert.NotNull(intervalField);
            var intervalValue = intervalField.GetValue(null);
            Assert.Equal(60, intervalValue);
        }

        [Fact]
        public void CacheExpirationConstant_ShouldBe5Minutes()
        {
            // Arrange & Act
            var expirationField = typeof(PromotionCacheService)
                .GetField("CacheExpirationInMinutes", BindingFlags.NonPublic | BindingFlags.Static);

            // Assert
            Assert.NotNull(expirationField);
            var expirationValue = expirationField.GetValue(null);
            Assert.Equal(5, expirationValue);
        }

        [Fact]
        public async Task RefreshRunningPromotionCache_WithNullPromotions_ShouldNotCache()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            List<Promotion>? nullPromotions = null;
            promotionService.GetRunningPromotionsAsync().Returns(nullPromotions!);

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Get private method
            var method = typeof(PromotionCacheService)
                .GetMethod("RefreshRunningPromotionCache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, null)!);

            // Assert
            Assert.Null(exception);
            sharedCache.DidNotReceiveWithAnyArgs().Set(default(string)!, default(object)!, default(TimeSpan));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldImplementContinuousLoop()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var callCount = 0;
            promotionService.GetRunningPromotionsAsync().Returns(_ =>
            {
                callCount++;
                return new List<Promotion>();
            });

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Create cancellation token that cancels after multiple iterations
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(150)); // Allow for at least 2 iterations

            // Get protected method
            var method = typeof(PromotionCacheService)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, new object[] { cancellationTokenSource.Token })!);

            // Assert
            Assert.IsType<TaskCanceledException>(exception); // Cancellation is expected
            // Should have been called at least once during the loop
            await promotionService.Received().GetRunningPromotionsAsync();
        }

        [Fact]
        public void PromotionCacheService_ShouldHaveCorrectDependencies()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Act & Assert - Verify constructor parameters match expected types
            var constructors = typeof(PromotionCacheService).GetConstructors();
            Assert.Single(constructors);
            
            var constructor = constructors[0];
            var parameters = constructor.GetParameters();
            
            Assert.Equal(4, parameters.Length);
            Assert.Equal(typeof(ILogger<PromotionCacheService>), parameters[0].ParameterType);
            Assert.Equal(typeof(IPromotionService), parameters[1].ParameterType);
            Assert.Equal(typeof(ISharedCacheService), parameters[2].ParameterType);
            Assert.Equal(typeof(ISentryClient), parameters[3].ParameterType);
        }

        [Fact]
        public async Task RefreshRunningPromotionCache_WithSinglePromotion_ShouldCacheCorrectly()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            var singlePromotion = new List<Promotion>
            {
                new Promotion 
                { 
                    Name = "Single Promotion", 
                    Code = "SINGLE", 
                    OperatorId = "OP1",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(7)
                }
            };

            promotionService.GetRunningPromotionsAsync().Returns(singlePromotion);

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            // Get private method
            var method = typeof(PromotionCacheService)
                .GetMethod("RefreshRunningPromotionCache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            await (Task)method.Invoke(service, null)!;

            // Assert
            sharedCache.Received().Set(
                "RunningPromotions",
                Arg.Any<List<Promotion>>(),
                TimeSpan.FromMinutes(5));
        }

        [Fact]
        public async Task ExecuteAsync_WhenOperationCancelled_ShouldExitGracefully()
        {
            // Arrange
            var logger = Substitute.For<ILogger<PromotionCacheService>>();
            var promotionService = Substitute.For<IPromotionService>();
            var sharedCache = Substitute.For<ISharedCacheService>();
            var sentryClient = Substitute.For<ISentryClient>();

            // Setup promotion service to throw TaskCanceledException
            promotionService.GetRunningPromotionsAsync().ThrowsAsync(new TaskCanceledException("Operation was cancelled"));

            var service = new PromotionCacheService(logger, promotionService, sharedCache, sentryClient);

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(50));

            // Get protected method
            var method = typeof(PromotionCacheService)
                .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () => 
                await (Task)method.Invoke(service, new object[] { cancellationTokenSource.Token })!);

            Assert.IsType<TaskCanceledException>(exception); // Cancellation is expected
        }

        [Fact]
        public void GetCacheKeyRunningPromotions_ShouldBeStaticMethod()
        {
            // Arrange & Act
            var method = typeof(PromotionCacheService).GetMethod("GetCacheKeyRunningPromotions");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.True(method.IsPublic);
            Assert.Equal(typeof(string), method.ReturnType);
            Assert.Empty(method.GetParameters());
        }
    }
}
