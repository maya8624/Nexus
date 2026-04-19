using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces
{
    public interface IAuthService
    {
        string ProviderName { get; }
        Task<Result<UserResponse>> Authenticate(string provider, string token, CancellationToken cancellationToken = default);
    }
}
