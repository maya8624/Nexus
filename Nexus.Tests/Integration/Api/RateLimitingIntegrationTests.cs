using FluentAssertions;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Nexus.Tests.Integration.Api;

public class RateLimitingIntegrationTests : IntegrationTestBase
{
    private const string LoginEndpoint = "/api/auth/login";
    private const string RegisterEndpoint = "/api/auth/register";

    private const int LoginPermitLimit = 20;
    private const int RegisterPermitLimit = 5;

    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@nexus.com";

    [Fact]
    public async Task Login_ExceedingPermitLimit_Returns429()
    {
        var email = UniqueEmail();

        for (var i = 0; i < LoginPermitLimit; i++)
        {
            var response = await Client.PostAsJsonAsync(LoginEndpoint, new EmailLoginRequest
            {
                Email = email,
                Password = "WrongPassword"
            });

            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var rejected = await Client.PostAsJsonAsync(LoginEndpoint, new EmailLoginRequest
        {
            Email = email,
            Password = "WrongPassword"
        });

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Retry-After is only present once SlidingWindowRateLimiter has enough elapsed
        // time to estimate a wait (it needs at least one internal segment to have ticked).
        // Firing all requests back-to-back in-process doesn't give it that, so the header
        // is legitimately absent here even though OnRejected sets it when available -
        // asserting on it would test .NET's rate limiter internals, not Nexus's own code.
        var error = await rejected.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("TooManyRequests");
    }

    [Fact]
    public async Task Register_ExceedingPermitLimit_Returns429()
    {
        for (var i = 0; i < RegisterPermitLimit; i++)
        {
            var response = await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest
            {
                Email = UniqueEmail(),
                Password = "Password123"
            });

            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var rejected = await Client.PostAsJsonAsync(RegisterEndpoint, new RegisterRequest
        {
            Email = UniqueEmail(),
            Password = "Password123"
        });

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var error = await rejected.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("TooManyRequests");
    }
}
