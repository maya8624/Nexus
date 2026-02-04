using System;
using System.Collections.Generic;
using System.Text;

namespace NexusPay.Application.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(string userId, string email);
    }
}
