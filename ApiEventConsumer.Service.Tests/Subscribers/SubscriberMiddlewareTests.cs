using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Service.Subscribers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class SubscriberMiddlewareTests
    {
        [Fact]
        public void AddSubscriberServices_ShouldBeExtensionMethod()
        {
            // Arrange
            // Act & Assert
            // Method exists and can be called (testing basic extension method functionality)
            var methodExists = typeof(SubscriberMiddleware).GetMethod("AddSubscriberServices");
            Assert.NotNull(methodExists);
            
            // Method should have correct signature
            Assert.True(methodExists.IsStatic);
            Assert.True(methodExists.IsPublic);
            
            var parameters = methodExists.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(IServiceCollection), parameters[0].ParameterType);
            Assert.Equal(typeof(IConfiguration), parameters[1].ParameterType);
        }

        [Fact]
        public void AddSubscriberServices_WithNullServices_ShouldThrow()
        {
            // Arrange
            IServiceCollection? nullServices = null;
            var configuration = Substitute.For<IConfiguration>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddSubscriberServices(configuration));
        }

        [Fact]
        public void AddSubscriberServices_ShouldAddServicesToCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Substitute.For<IConfiguration>();
            var initialCount = services.Count;

            // Act - Only call registration part, expect it to fail later due to missing dependencies
            var exception = Record.Exception(() => services.AddSubscriberServices(configuration));

            // Assert 
            // Should fail due to missing dependencies, but should have added services to collection first
            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Unable to resolve service for type", exception.Message);
            
            // However, some services should have been added before the failure
            Assert.True(services.Count > initialCount);
        }

        [Fact]
        public void AddSubscriberServices_ServiceDescriptors_ShouldContainExpectedTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Substitute.For<IConfiguration>();

            // Act - Try to add services, expect failure but some registrations should occur first
            try 
            {
                services.AddSubscriberServices(configuration);
            }
            catch (InvalidOperationException)
            {
                // Expected due to missing dependencies
            }

            // Assert - Check that some services were registered before failure
            var serviceTypes = services.Select(s => s.ServiceType).ToHashSet();
            
            // These should be registered early before the failure
            Assert.Contains(typeof(IMemoryCache), serviceTypes);
            Assert.Contains(typeof(ISharedCacheService), serviceTypes);
        }

        [Fact]
        public void AddSubscriberServices_ShouldRegisterMemoryCacheFirst()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Substitute.For<IConfiguration>();

            // Act - Try registration
            try 
            {
                services.AddSubscriberServices(configuration);
            }
            catch (InvalidOperationException)
            {
                // Expected due to missing dependencies
            }

            // Assert - Memory cache should be one of the first registrations
            var memoryCacheDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IMemoryCache));
            Assert.NotNull(memoryCacheDescriptor);
        }

        [Fact]
        public void AddSubscriberServices_MiddlewareClassShouldExist()
        {
            // Arrange & Act & Assert
            var middlewareType = typeof(SubscriberMiddleware);
            
            Assert.NotNull(middlewareType);
            Assert.True(middlewareType.IsClass);
            Assert.True(middlewareType.IsAbstract); // Static class
            Assert.True(middlewareType.IsSealed);   // Static class
        }

        [Fact]
        public void AddSubscriberServices_ShouldHaveCorrectNamespace()
        {
            // Arrange & Act & Assert
            var middlewareType = typeof(SubscriberMiddleware);
            
            Assert.Equal("ApiEventConsumer.Service.Subscribers", middlewareType.Namespace);
        }

        [Fact]
        public void AddSubscriberServices_ShouldBeStaticExtensionMethod()
        {
            // Arrange & Act & Assert
            var method = typeof(SubscriberMiddleware).GetMethod("AddSubscriberServices");
            
            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.True(method.IsPublic);
            
            // Check if it's an extension method (first parameter has 'this' modifier)
            var firstParam = method.GetParameters().FirstOrDefault();
            Assert.NotNull(firstParam);
            Assert.True(firstParam.GetCustomAttributes(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false).Any() 
                       || method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false).Any());
        }

        [Fact] 
        public void AddSubscriberServices_ShouldReturnVoid()
        {
            // Arrange & Act & Assert
            var method = typeof(SubscriberMiddleware).GetMethod("AddSubscriberServices");
            
            Assert.NotNull(method);
            Assert.Equal(typeof(void), method.ReturnType);
        }
    }
}