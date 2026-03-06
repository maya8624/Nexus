using Nexus.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(string userId, string email);
        void DeleteTokenCookie();
        UserResponse? GetCurrentUser();
    }
}
