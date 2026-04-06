using Moq;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Xunit;

namespace Nexus.Tests.Application
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IPasswordHasherService> _passwordHasherMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _passwordHasherMock = new Mock<IPasswordHasherService>();
            _tokenServiceMock = new Mock<ITokenService>();

            _userService = new UserService(
                _userRepoMock.Object,
                _uowMock.Object,
                _passwordHasherMock.Object,
                _tokenServiceMock.Object
            );
        }

        [Fact]
        public async Task RegisterEmailUser_WithUniqueEmail_ShouldReturnUserResponse()
        {
            // Arrange
            var email = "test@example.com";
            var password = "SecurePassword123";
            var hashedPassword = "hashed_password_xyz";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(password))
                .Returns(hashedPassword);

            _userRepoMock
                .Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            Assert.NotNull(result.UserId);

            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.HashPassword(password), Times.Once);
            _userRepoMock.Verify(x => x.Create(It.Is<User>(u =>
                u.Email == email &&
                u.PasswordHash == hashedPassword &&
                u.Id != Guid.Empty &&
                u.CreatedAtUtc != default
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterEmailUser_WithExistingEmail_ShouldThrowUserException()
        {
            // Arrange
            var email = "existing@example.com";
            var password = "SecurePassword123";

            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = "existing_hash",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(
                () => _userService.RegisterEmailUser(email, password)
            );

            Assert.Equal("Email already registered", exception.Message);
            
            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
            _userRepoMock.Verify(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RegisterEmailUser_WithPlaintextPassword_ShouldHashPasswordBeforeSaving()
        {
            // Arrange
            var email = "test@example.com";
            var password = "PlainTextPassword";
            var hashedPassword = "super_secure_hashed_password";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(password))
                .Returns(hashedPassword);

            _userRepoMock
                .Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _userService.RegisterEmailUser(email, password);

            // Assert
            _passwordHasherMock.Verify(x => x.HashPassword(password), Times.Once);
            _userRepoMock.Verify(x => x.Create(It.Is<User>(u => 
                u.PasswordHash == hashedPassword && 
                u.PasswordHash != password
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterEmailUser_WithValidInput_ShouldCreateUserWithExpectedProperties()
        {
            // Arrange
            var email = "newuser@example.com";
            var password = "MyPassword123";
            var hashedPassword = "hashed_value";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(password))
                .Returns(hashedPassword);

            User? capturedUser = null;
            _userRepoMock
                .Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((u, ct) => capturedUser = u)
                .Returns(Task.CompletedTask);

            // Act
            await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.NotNull(capturedUser);
            Assert.Equal(email, capturedUser.Email);
            Assert.Equal(hashedPassword, capturedUser.PasswordHash);
            Assert.NotEqual(Guid.Empty, capturedUser.Id);
            Assert.True(capturedUser.CreatedAtUtc <= DateTimeOffset.UtcNow);
            Assert.True(capturedUser.CreatedAtUtc >= DateTimeOffset.UtcNow.AddSeconds(-5));
        }

        [Theory]
        [InlineData("user1@example.com", "Password123")]
        [InlineData("user2@test.com", "AnotherPassword456")]
        [InlineData("admin@company.com", "SuperSecure789")]
        public async Task RegisterEmailUser_WithMultipleValidInputs_ShouldReturnUserResponse(string email, string password)
        {
            // Arrange
            var hashedPassword = $"hashed_{password}";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(password))
                .Returns(hashedPassword);

            _userRepoMock
                .Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            Assert.NotNull(result.UserId);
        }

        [Fact]
        public async Task RegisterEmailUser_WithValidInput_ShouldReturnGuidStringUserId()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";
            var hashedPassword = "hashed_password";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(password))
                .Returns(hashedPassword);

            _userRepoMock
                .Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.NotNull(result.UserId);
            Assert.True(Guid.TryParse(result.UserId, out _), "UserId should be a valid GUID string");
        }

        [Fact]
        public async Task RegisterEmailUser_WithValidInput_ShouldCallRepositoryCreateOnce()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";
            var hashedPassword = "hashed_password";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.HashPassword(password))
                .Returns(hashedPassword);

            _userRepoMock
                .Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _userService.RegisterEmailUser(email, password);

            // Assert
            _userRepoMock.Verify(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var password = "SecurePassword123";
            var hashedPassword = "hashed_password";
            var userId = Guid.NewGuid();
            var expectedToken = "jwt_token_xyz";

            var user = new User
            {
                Id = userId,
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(hashedPassword, password))
                .Returns(true);

            _tokenServiceMock
                .Setup(x => x.CreateToken(userId.ToString(), email))
                .Returns(expectedToken);

            // Act
            var result = await _userService.Login(email, password);

            // Assert
            Assert.True(result);
            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.VerifyPassword(hashedPassword, password), Times.Once);
            _tokenServiceMock.Verify(x => x.CreateToken(userId.ToString(), email), Times.Once);
        }

        [Fact]
        public async Task Login_WithMissingUser_ShouldReturnFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "SomePassword123";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.Login(email, password);

            // Assert
            Assert.False(result);
            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var email = "test@example.com";
            var wrongPassword = "WrongPassword123";
            var hashedPassword = "hashed_password";

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(hashedPassword, wrongPassword))
                .Returns(false);

            // Act
            var result = await _userService.Login(email, wrongPassword);

            // Assert
            Assert.False(result);
            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.VerifyPassword(hashedPassword, wrongPassword), Times.Once);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldCreateTokenWithExpectedParameters()
        {
            // Arrange
            var email = "test@example.com";
            var password = "SecurePassword123";
            var hashedPassword = "hashed_password";
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(hashedPassword, password))
                .Returns(true);

            _tokenServiceMock
                .Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("token");

            // Act
            await _userService.Login(email, password);

            // Assert
            _tokenServiceMock.Verify(x => x.CreateToken(
                userId.ToString(),
                email
            ), Times.Once);
        }

        [Theory]
        [InlineData("user1@example.com", "Password123")]
        [InlineData("user2@test.com", "AnotherPassword456")]
        [InlineData("admin@company.com", "SuperSecure789")]
        public async Task Login_WithMultipleValidCredentials_ShouldReturnTrue(string email, string password)
        {
            // Arrange
            var hashedPassword = $"hashed_{password}";
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(hashedPassword, password))
                .Returns(true);

            _tokenServiceMock
                .Setup(x => x.CreateToken(userId.ToString(), email))
                .Returns("token");

            // Act
            var result = await _userService.Login(email, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Login_WithMissingUser_ShouldNotCreateToken()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "SomePassword123";

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync((User?)null);

            // Act
            await _userService.Login(email, password);

            // Assert
            _tokenServiceMock.Verify(x => x.CreateToken(
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Never);
        }

        [Fact]
        public async Task Login_WithFailedPasswordVerification_ShouldNotCreateToken()
        {
            // Arrange
            var email = "test@example.com";
            var password = "WrongPassword";
            var hashedPassword = "hashed_password";

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(hashedPassword, password))
                .Returns(false);

            // Act
            await _userService.Login(email, password);

            // Assert
            _tokenServiceMock.Verify(x => x.CreateToken(
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Never);
        }

        [Fact]
        public async Task Login_WithAnyCredentials_ShouldQueryUserBeforeVerifyingPassword()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";
            var callSequence = new List<string>();

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .Callback(() => callSequence.Add("GetByEmail"))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => callSequence.Add("VerifyPassword"))
                .Returns(true);

            // Act
            await _userService.Login(email, password);

            // Assert
            Assert.Single(callSequence);
            Assert.Equal("GetByEmail", callSequence[0]);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldVerifyPasswordBeforeCreatingToken()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";
            var hashedPassword = "hashed_password";
            var callSequence = new List<string>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _userRepoMock
                .Setup(x => x.GetByEmail(email))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword(hashedPassword, password))
                .Callback(() => callSequence.Add("VerifyPassword"))
                .Returns(true);

            _tokenServiceMock
                .Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => callSequence.Add("CreateToken"))
                .Returns("token");

            // Act
            await _userService.Login(email, password);

            // Assert
            Assert.Equal(2, callSequence.Count);
            Assert.Equal("VerifyPassword", callSequence[0]);
            Assert.Equal("CreateToken", callSequence[1]);
        }

        #endregion
    }
}
