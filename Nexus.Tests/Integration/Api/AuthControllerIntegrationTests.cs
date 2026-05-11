using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Nexus.Tests.Integration.Api;

public class AuthControllerIntegrationTests : IntegrationTestBase
{
    private const string LoginEndpoint    = "/api/auth/login";
    private const string RegisterEndpoint = "/api/auth/register";
    private const string RefreshEndpoint  = "/api/auth/refresh";

    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@nexus.com";

    private static string HashToken(string raw)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    #region POST /api/auth/register

    [Fact]
    public async Task Register_WithValidRequest_Returns201AndBothTokens()
    {
        var request = new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123",
            FirstName = "Jane",
            LastName = "Doe"
        };

        var response = await Client.PostAsJsonAsync(RegisterEndpoint, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Email.Should().Be(request.Email);
        body.Token.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = UniqueEmail();
        var request = new RegisterRequest { Email = email, Password = "Password123" };

        await Client.PostAsJsonAsync(RegisterEndpoint, request);
        var response = await Client.PostAsJsonAsync(RegisterEndpoint, request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("EMAIL_TAKEN");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var request = new RegisterRequest { Email = "not-an-email", Password = "Password123" };

        var response = await Client.PostAsJsonAsync(RegisterEndpoint, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var request = new RegisterRequest { Email = UniqueEmail(), Password = "abc" };

        var response = await Client.PostAsJsonAsync(RegisterEndpoint, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/auth/login

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndBothTokens()
    {
        var email = UniqueEmail();
        await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest { Email = email, Password = "Password123" });

        var response = await Client.PostAsJsonAsync(LoginEndpoint, new EmailLoginRequest
        {
            Email = email,
            Password = "Password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        body!.Token.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = UniqueEmail();
        await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest { Email = email, Password = "Password123" });

        var response = await Client.PostAsJsonAsync(LoginEndpoint, new EmailLoginRequest
        {
            Email = email,
            Password = "WrongPassword"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync(LoginEndpoint, new EmailLoginRequest
        {
            Email = UniqueEmail(),
            Password = "Password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithMissingEmail_Returns400()
    {
        var response = await Client.PostAsJsonAsync(LoginEndpoint, new EmailLoginRequest
        {
            Email = "",
            Password = "Password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/auth/refresh

    [Fact]
    public async Task Refresh_WithValidToken_Returns200AndNewTokens()
    {
        var registerResponse = await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123"
        });
        var registered = await registerResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        var response = await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest
        {
            RefreshToken = registered!.RefreshToken!
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        body!.Token.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(registered.RefreshToken);
        body.Token.Should().NotBe(registered.Token);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsCorrectUserData()
    {
        var email = UniqueEmail();
        var registerResponse = await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest
        {
            Email = email,
            Password = "Password123",
            FirstName = "Jane",
            LastName = "Doe"
        });
        var registered = await registerResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        var response = await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest
        {
            RefreshToken = registered!.RefreshToken!
        });

        var body = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        body!.Email.Should().Be(email);
        body.UserId.Should().Be(registered.UserId);
        body.FirstName.Should().Be("Jane");
        body.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task Refresh_IsRotated_OldTokenCannotBeReused()
    {
        var registerResponse = await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123"
        });
        var registered = await registerResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        var originalRefreshToken = registered!.RefreshToken!;

        // First refresh succeeds and rotates the token
        await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest { RefreshToken = originalRefreshToken });

        // Attempt to reuse the original token
        var response = await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_WithNonExistentToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest
        {
            RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_Returns401()
    {
        var registerResponse = await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123"
        });
        var registered = await registerResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        var rawToken = registered!.RefreshToken!;

        // Revoke the token directly in the DB
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.RefreshTokens.FirstAsync(x => x.TokenHash == HashToken(rawToken));
        stored.IsRevoked = true;
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest
        {
            RefreshToken = rawToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_Returns401()
    {
        var registerResponse = await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123"
        });
        var registered = await registerResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        var rawToken = registered!.RefreshToken!;

        // Expire the token directly in the DB
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.RefreshTokens.FirstAsync(x => x.TokenHash == HashToken(rawToken));
        stored.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest
        {
            RefreshToken = rawToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task Refresh_WithMissingBody_Returns400()
    {
        var response = await Client.PostAsJsonAsync(RefreshEndpoint, new RefreshTokenRequest
        {
            RefreshToken = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
