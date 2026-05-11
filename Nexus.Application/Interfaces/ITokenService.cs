using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(string userId, string email, string? firstName = null, string? lastName = null);
        UserResponse? GetCurrentUser();
        string GenerateRefreshToken();
        string HashToken(string token);
    }
}
