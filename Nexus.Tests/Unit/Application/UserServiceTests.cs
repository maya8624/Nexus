using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IPasswordHasherService> _passwordHasherMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _passwordHasherMock = new Mock<IPasswordHasherService>();
            _tokenServiceMock = new Mock<ITokenService>();

            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("raw_refresh_token");
            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.Create(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);

            var jwtOptions = Options.Create(new JwtSettings { RefreshTokenExpiryDays = 7 });

            _userService = new UserService(
                _userRepoMock.Object,
                _refreshTokenRepoMock.Object,
                _uowMock.Object,
                _passwordHasherMock.Object,
                _tokenServiceMock.Object,
                jwtOptions
            );
        }

        #region RegisterEmailUser Tests

        [Fact]
        public async Task RegisterEmailUser_WithUniqueEmail_ShouldReturnSuccessResult()
        {
            // Arrange
            var email = "test@example.com";
            var password = "SecurePassword123";
            var hashedPassword = "hashed_password_xyz";

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(x => x.HashPassword(password)).Returns(hashedPassword);
            _userRepoMock.Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), email, It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

            // Act
            var result = await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(email, result.Value!.Email);
            Assert.NotNull(result.Value.UserId);

            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.HashPassword(password), Times.Once);
            _userRepoMock.Verify(x => x.Create(It.Is<User>(u =>
                u.Email == email &&
                u.PasswordHash == hashedPassword &&
                u.Id != Guid.Empty &&
                u.CreatedAtUtc != default
            ), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), email, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task RegisterEmailUser_WithExistingEmail_ShouldReturnConflictResult()
        {
            // Arrange
            var email = "existing@example.com";
            var password = "SecurePassword123";

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = "existing_hash",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            // Act
            var result = await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("EMAIL_TAKEN", result.Errors[0].Code);
            Assert.Equal("Email already registered", result.Errors[0].Message);

            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
            _userRepoMock.Verify(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task RegisterEmailUser_WithPlaintextPassword_ShouldHashPasswordBeforeSaving()
        {
            // Arrange
            var email = "test@example.com";
            var password = "PlainTextPassword";
            var hashedPassword = "super_secure_hashed_password";

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(x => x.HashPassword(password)).Returns(hashedPassword);
            _userRepoMock.Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

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

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(x => x.HashPassword(password)).Returns(hashedPassword);

            User? capturedUser = null;
            _userRepoMock
                .Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((u, ct) => capturedUser = u)
                .Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

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
        public async Task RegisterEmailUser_WithMultipleValidInputs_ShouldReturnSuccessResult(string email, string password)
        {
            // Arrange
            var hashedPassword = $"hashed_{password}";

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(x => x.HashPassword(password)).Returns(hashedPassword);
            _userRepoMock.Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

            // Act
            var result = await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(email, result.Value!.Email);
            Assert.NotNull(result.Value.UserId);
        }

        [Fact]
        public async Task RegisterEmailUser_WithValidInput_ShouldReturnGuidStringUserId()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(x => x.HashPassword(password)).Returns("hashed_password");
            _userRepoMock.Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

            // Act
            var result = await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(Guid.TryParse(result.Value!.UserId, out _), "UserId should be a valid GUID string");
        }

        [Fact]
        public async Task RegisterEmailUser_WithValidInput_ShouldCallRepositoryCreateOnce()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(x => x.HashPassword(password)).Returns("hashed_password");
            _userRepoMock.Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

            // Act
            await _userService.RegisterEmailUser(email, password);

            // Assert
            _userRepoMock.Verify(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterEmailUser_WithValidInput_ShouldSaveBeforeIssuingToken()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";
            var callSequence = new List<string>();

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);
            _passwordHasherMock.Setup(x => x.HashPassword(password)).Returns("hashed_password");
            _userRepoMock.Setup(x => x.Create(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uowMock.Setup(x => x.SaveChanges()).Callback(() => callSequence.Add("SaveChanges")).ReturnsAsync(1);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Callback(() => callSequence.Add("CreateToken"))
                .Returns("token");

            // Act
            await _userService.RegisterEmailUser(email, password);

            // Assert
            Assert.Equal(2, callSequence.Count);
            Assert.Equal("SaveChanges", callSequence[0]);
            Assert.Equal("CreateToken", callSequence[1]);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnSuccessResult()
        {
            // Arrange
            var email = "test@example.com";
            var password = "SecurePassword123";
            var hashedPassword = "hashed_password";
            var userId = Guid.NewGuid();

            var user = new User { Id = userId, Email = email, PasswordHash = hashedPassword, CreatedAtUtc = DateTimeOffset.UtcNow };

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(hashedPassword, password)).Returns(true);
            _tokenServiceMock.Setup(x => x.CreateToken(userId.ToString(), email, It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

            // Act
            var result = await _userService.Login(email, password);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(email, result.Value!.Email);
            Assert.Equal(userId.ToString(), result.Value.UserId);

            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.VerifyPassword(hashedPassword, password), Times.Once);
            _tokenServiceMock.Verify(x => x.CreateToken(userId.ToString(), email, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public async Task Login_WithMissingUser_ShouldReturnUnauthorizedResult()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "SomePassword123";

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.Login(email, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal("INVALID_CREDENTIALS", result.Errors[0].Code);

            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ShouldReturnUnauthorizedResult()
        {
            // Arrange
            var email = "test@example.com";
            var wrongPassword = "WrongPassword123";
            var hashedPassword = "hashed_password";

            var user = new User { Id = Guid.NewGuid(), Email = email, PasswordHash = hashedPassword, CreatedAtUtc = DateTimeOffset.UtcNow };

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(hashedPassword, wrongPassword)).Returns(false);

            // Act
            var result = await _userService.Login(email, wrongPassword);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal("INVALID_CREDENTIALS", result.Errors[0].Code);

            _userRepoMock.Verify(x => x.GetByEmail(email), Times.Once);
            _passwordHasherMock.Verify(x => x.VerifyPassword(hashedPassword, wrongPassword), Times.Once);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldCreateTokenWithExpectedParameters()
        {
            // Arrange
            var email = "test@example.com";
            var password = "SecurePassword123";
            var hashedPassword = "hashed_password";
            var userId = Guid.NewGuid();

            var user = new User { Id = userId, Email = email, PasswordHash = hashedPassword, CreatedAtUtc = DateTimeOffset.UtcNow };

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(hashedPassword, password)).Returns(true);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

            // Act
            await _userService.Login(email, password);

            // Assert
            _tokenServiceMock.Verify(x => x.CreateToken(userId.ToString(), email, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        }

        [Theory]
        [InlineData("user1@example.com", "Password123")]
        [InlineData("user2@test.com", "AnotherPassword456")]
        [InlineData("admin@company.com", "SuperSecure789")]
        public async Task Login_WithMultipleValidCredentials_ShouldReturnSuccessResult(string email, string password)
        {
            // Arrange
            var hashedPassword = $"hashed_{password}";
            var userId = Guid.NewGuid();

            var user = new User { Id = userId, Email = email, PasswordHash = hashedPassword, CreatedAtUtc = DateTimeOffset.UtcNow };

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(hashedPassword, password)).Returns(true);
            _tokenServiceMock.Setup(x => x.CreateToken(userId.ToString(), email, It.IsAny<string?>(), It.IsAny<string?>())).Returns("token");

            // Act
            var result = await _userService.Login(email, password);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Login_WithMissingUser_ShouldNotCreateToken()
        {
            // Arrange
            _userRepoMock.Setup(x => x.GetByEmail(It.IsAny<string>())).ReturnsAsync((User?)null);

            // Act
            await _userService.Login("nonexistent@example.com", "SomePassword123");

            // Assert
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithFailedPasswordVerification_ShouldNotCreateToken()
        {
            // Arrange
            var email = "test@example.com";
            var password = "WrongPassword";
            var hashedPassword = "hashed_password";

            var user = new User { Id = Guid.NewGuid(), Email = email, PasswordHash = hashedPassword, CreatedAtUtc = DateTimeOffset.UtcNow };

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(hashedPassword, password)).Returns(false);

            // Act
            await _userService.Login(email, password);

            // Assert
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithAnyCredentials_ShouldQueryUserBeforeVerifyingPassword()
        {
            // Arrange
            var email = "test@example.com";
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
            await _userService.Login(email, "Password123");

            // Assert — user is null so VerifyPassword is short-circuited
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

            var user = new User { Id = Guid.NewGuid(), Email = email, PasswordHash = hashedPassword, CreatedAtUtc = DateTimeOffset.UtcNow };

            _userRepoMock.Setup(x => x.GetByEmail(email)).ReturnsAsync(user);
            _passwordHasherMock
                .Setup(x => x.VerifyPassword(hashedPassword, password))
                .Callback(() => callSequence.Add("VerifyPassword"))
                .Returns(true);
            _tokenServiceMock
                .Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
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

        #region RefreshAsync Tests

        private static RefreshToken ValidStoredToken(Guid userId, User user) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hashedtoken",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(6),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsRevoked = false,
            User = user,
        };

        private static User SampleUser(Guid? id = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            PasswordHash = "hash",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        [Fact]
        public async Task RefreshAsync_WithValidToken_ShouldReturnSuccess()
        {
            // Arrange
            var user = SampleUser();
            var stored = ValidStoredToken(user.Id, user);

            _tokenServiceMock.Setup(x => x.HashToken("incoming_token")).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
            _tokenServiceMock.Setup(x => x.CreateToken(user.Id.ToString(), user.Email, user.FirstName, user.LastName)).Returns("new_access_token");

            // Act
            var result = await _userService.RefreshAsync("incoming_token");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("new_access_token", result.Value!.Token);
            Assert.Equal(user.Email, result.Value.Email);
            Assert.Equal(user.Id.ToString(), result.Value.UserId);
        }

        [Fact]
        public async Task RefreshAsync_WithValidToken_ShouldReturnNewRefreshToken()
        {
            // Arrange
            var user = SampleUser();
            var stored = ValidStoredToken(user.Id, user);

            _tokenServiceMock.Setup(x => x.HashToken("incoming_token")).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("brand_new_raw_token");
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("access");

            // Act
            var result = await _userService.RefreshAsync("incoming_token");

            // Assert
            Assert.Equal("brand_new_raw_token", result.Value!.RefreshToken);
        }

        [Fact]
        public async Task RefreshAsync_WithValidToken_ShouldRevokeOldToken()
        {
            // Arrange
            var user = SampleUser();
            var stored = ValidStoredToken(user.Id, user);

            _tokenServiceMock.Setup(x => x.HashToken("incoming_token")).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("access");

            // Act
            await _userService.RefreshAsync("incoming_token");

            // Assert
            Assert.True(stored.IsRevoked);
        }

        [Fact]
        public async Task RefreshAsync_WithValidToken_ShouldPersistNewRefreshToken()
        {
            // Arrange
            var user = SampleUser();
            var stored = ValidStoredToken(user.Id, user);

            _tokenServiceMock.Setup(x => x.HashToken("incoming_token")).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("access");

            // Act
            await _userService.RefreshAsync("incoming_token");

            // Assert
            _refreshTokenRepoMock.Verify(x => x.Create(It.Is<RefreshToken>(t => t.UserId == user.Id && !t.IsRevoked), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_WithNonExistentToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken?)null);

            // Act
            var result = await _userService.RefreshAsync("ghost_token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal("INVALID_REFRESH_TOKEN", result.Errors[0].Code);
        }

        [Fact]
        public async Task RefreshAsync_WithRevokedToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var user = SampleUser();
            var stored = ValidStoredToken(user.Id, user);
            stored.IsRevoked = true;

            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

            // Act
            var result = await _userService.RefreshAsync("revoked_token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal("INVALID_REFRESH_TOKEN", result.Errors[0].Code);
        }

        [Fact]
        public async Task RefreshAsync_WithExpiredToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var user = SampleUser();
            var stored = ValidStoredToken(user.Id, user);
            stored.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);

            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

            // Act
            var result = await _userService.RefreshAsync("expired_token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal("INVALID_REFRESH_TOKEN", result.Errors[0].Code);
        }

        [Fact]
        public async Task RefreshAsync_WithInvalidToken_ShouldNotCreateNewToken()
        {
            // Arrange
            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken?)null);

            // Act
            await _userService.RefreshAsync("bad_token");

            // Assert
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
            _refreshTokenRepoMock.Verify(x => x.Create(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WithValidToken_ShouldUseUserDataFromStoredToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = SampleUser(userId);
            var stored = ValidStoredToken(userId, user);

            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("access");

            // Act
            await _userService.RefreshAsync("token");

            // Assert — token is created with the stored user's identity, not from a separate DB lookup
            _tokenServiceMock.Verify(x => x.CreateToken(userId.ToString(), user.Email, user.FirstName, user.LastName), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_WithValidToken_NewRefreshTokenShouldBelongToSameUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = SampleUser(userId);
            var stored = ValidStoredToken(userId, user);

            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashedtoken");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHash("hashedtoken", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns("access");

            RefreshToken? persisted = null;
            _refreshTokenRepoMock
                .Setup(x => x.Create(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((t, _) => persisted = t)
                .Returns(Task.CompletedTask);

            // Act
            await _userService.RefreshAsync("token");

            // Assert
            Assert.NotNull(persisted);
            Assert.Equal(userId, persisted.UserId);
            Assert.False(persisted.IsRevoked);
            Assert.True(persisted.ExpiresAt > DateTimeOffset.UtcNow);
        }

        #endregion
    }
}
