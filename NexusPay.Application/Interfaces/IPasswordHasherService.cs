using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPay.Application.Interfaces
{
    public interface IPasswordHasherService
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}
