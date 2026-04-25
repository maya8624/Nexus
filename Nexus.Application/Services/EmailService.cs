using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;

namespace Nexus.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtp, ILogger<EmailService> logger)
        {
            _smtp = smtp.Value;
            _logger = logger;
        }

        public Task SendDepositConfirmationAsync(Deposit deposit, CancellationToken ct)
        {
            _logger.LogInformation(
                "[STUB] Deposit confirmation — DepositId={DepositId}, UserId={UserId}, Amount={Amount} AUD, PropertyId={PropertyId}",
                deposit.Id, deposit.UserId, deposit.Amount, deposit.PropertyId);

            return Task.CompletedTask;
        }

        public async Task SendBookingConfirmationAsync(string toEmail, string toName, Guid bookingId, DateTimeOffset slotStart, DateTimeOffset slotEnd, CancellationToken ct)
        {
            var subject = "Inspection Booking Confirmed";
            var body = $"""
                Hi {toName},

                Your inspection booking has been confirmed.

                Booking ID : {bookingId}
                Date       : {slotStart:dddd, dd MMMM yyyy}
                Time       : {slotStart:hh:mm tt} – {slotEnd:hh:mm tt} (UTC)

                If you need to cancel, please do so through the app.

                Regards,
                Nexus
                """;

            await SendAsync(toEmail, toName, subject, body, ct);
        }

        public async Task SendBookingCancellationAsync(string toEmail, string toName, Guid bookingId, CancellationToken ct)
        {
            var subject = "Inspection Booking Cancelled";
            var body = $"""
                Hi {toName},

                Your inspection booking has been cancelled.

                Booking ID : {bookingId}

                If this was a mistake, you can create a new booking through the app.

                Regards,
                Nexus
                """;

            await SendAsync(toEmail, toName, subject, body, ct);
        }

        private async Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken ct)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {ToEmail} — Subject: {Subject}", toEmail, subject);
        }
    }
}
