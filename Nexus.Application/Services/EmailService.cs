using Microsoft.Extensions.Logging;
using Nexus.Application.Interfaces.Business;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendDepositConfirmationAsync(Deposit deposit, CancellationToken ct)
        {
            _logger.LogInformation(
                "[STUB] Deposit confirmation — DepositId={DepositId}, UserId={UserId}, Amount={Amount} AUD, PropertyId={PropertyId}",
                deposit.Id, deposit.UserId, deposit.Amount, deposit.PropertyId);

            return Task.CompletedTask;
        }
    }
}
