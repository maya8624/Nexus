using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces.Business
{
    public interface IDepositService
    {
        Task<Result<DepositResponse>> CreateCheckoutSessionAsync(CreateDepositRequest request, Guid userId, CancellationToken ct);
        Task FulfillDepositAsync(string stripeSessionId, CancellationToken ct);
        Task<Result<DepositResponse?>> GetMyDeposit(Guid listingId, Guid userId, CancellationToken ct);
    }
}
