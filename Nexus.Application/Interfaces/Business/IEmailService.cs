using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Business
{
    public interface IEmailService
    {
        Task SendDepositConfirmationAsync(Deposit deposit, CancellationToken ct);
    }
}
