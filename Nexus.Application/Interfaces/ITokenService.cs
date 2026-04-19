using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(string userId, string email, string? firstName = null, string? lastName = null);
        void DeleteTokenCookie();
        UserResponse? GetCurrentUser();
    }
}
