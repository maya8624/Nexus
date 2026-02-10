using Moq;
using NexusPay.Application.Factories;
using NexusPay.Application.Interfaces;
using Xunit;

namespace NexusPay.Tests.Application
{
    public class AuthFactoryTests
    {
        private readonly Mock<IAuthService> _googleServiceMock;
        private readonly Mock<IAuthService> _microsoftServiceMock;
        private readonly AuthServiceFactory _factory;

        public AuthFactoryTests()
        {
            _googleServiceMock = new Mock<IAuthService>();
            _googleServiceMock.Setup(s => s.ProviderName).Returns("google");

            _microsoftServiceMock = new Mock<IAuthService>();
            _microsoftServiceMock.Setup(s => s.ProviderName).Returns("microsoft");

            // Create a list of all mocks
            var services = new List<IAuthService>
            {
                _googleServiceMock.Object,
                _microsoftServiceMock.Object
            };

            // Inject the list into the factory
            _factory = new AuthServiceFactory(services);
        }

        [Fact]
        public void GetAuthProvider_ShouldReturnCorrectService_WhenProviderExists()
        {
            // Act
            var result = _factory.GetAuthProvider("google");

            // Assert
            Assert.Equal(_googleServiceMock.Object, result);
        }

        [Fact]
        public void GetAuthProvider_ShouldThrowException_WhenProviderDoesNotExist()
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => _factory.GetAuthProvider("apple"));
        }
    }
}
