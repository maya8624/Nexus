using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IRefreshTokenRepository: IRepositoryBase<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenHash(string tokenHash, CancellationToken ct = default);
    }
}
