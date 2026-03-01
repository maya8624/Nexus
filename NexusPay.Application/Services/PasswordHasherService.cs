using Microsoft.AspNetCore.Identity;
using NexusPay.Application.Interfaces;
using NexusPay.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPay.Application.Services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private readonly PasswordHasher<User> _hasher;

        public PasswordHasherService()
        {
            _hasher = new PasswordHasher<User>();
        }

        public string HashPassword(string password)
        {
            return _hasher.HashPassword(null!, password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(
                null!,
                hashedPassword,
                providedPassword
            );

            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
