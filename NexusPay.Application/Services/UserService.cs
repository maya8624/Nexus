using NexusPay.Application.Dtos;
using NexusPay.Application.Exceptions;
using NexusPay.Application.Interfaces;
using NexusPay.Domain.Entities;
using NexusPay.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPay.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IUnitOfWork _uow;

        public UserService(IUserRepository userRepo, IUnitOfWork uow)
        {
            _userRepo = userRepo;
            _uow = uow;
        }

        //TODO: hash password
        public async Task<User> CreateEmailUser(string email, string password, string? name)
        {
            var existing = await _userRepo.GetByEmail(email);

            if (existing != null)
                throw new UserException("Email already registered");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = name,
                PasswordHash = password,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await _userRepo.Create(user);
            return user;
        }

        public async Task<User> CreateAuthUser(ExternalUserResponse externalUser, string providerName)
        {
            var existing = await _userRepo.GetByEmail(externalUser.Email);

            if (existing != null)
                return existing;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = externalUser.Email,
                Name = externalUser.Name,
                CreatedAt = DateTimeOffset.UtcNow,
                Logins =
                [
                    new() {
                        Id = Guid.NewGuid(),
                        Provider = providerName,
                        ProviderKey = externalUser.ProviderKey,
                        LastLoginAt = DateTimeOffset.UtcNow,
                    }
                ]
            };

            await _userRepo.Create(user);
            await _uow.SaveChanges();
            return user;
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _userRepo.GetByEmail(email);
        }

        //public async Task<User?> GetByProviderId(string provider, string providerUserId)
        //{
        //    return await _userRepo.GetByProviderId(provider, providerUserId);
        //}

        public async Task<User?> GetEmailUser(string email, string password)
        {
            return await _userRepo.GetEmailUser(email, password);
        }
    }
}
