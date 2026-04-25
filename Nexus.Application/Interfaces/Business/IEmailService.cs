using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Business
{
    public interface IEmailService
    {
        Task SendDepositConfirmationAsync(Deposit deposit, CancellationToken ct);
        Task SendBookingConfirmationAsync(string toEmail, string toName, Guid bookingId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken ct);
        Task SendBookingCancellationAsync(string toEmail, string toName, Guid bookingId, CancellationToken ct);
    }
}
