using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Business
{
    public interface IUserService
    {
        Task<User> CreateAuthUser(ExternalUserResponse externalUser, string provider, CancellationToken cancellationToken = default);
        Task<Result<UserResponse>> RegisterEmailUser(string email, string password, CancellationToken cancellationToken = default);
        Task<Result<UserResponse>> Login(string email, string password, CancellationToken cancellationToken = default);
    }
}
