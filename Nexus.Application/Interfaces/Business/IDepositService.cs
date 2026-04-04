using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces.Business
{
    public interface IDepositService
    {
        Task<Result<DepositResponse>> CreateCheckoutSessionAsync(CreateDepositRequest request, CancellationToken ct);
        Task FulfillDepositAsync(string stripeSessionId, CancellationToken ct);
    }
}
