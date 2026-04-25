# Todo

## Inspection Booking Emails

Send an email to the user after a booking is created or cancelled.

- [ ] Choose email provider (MailKit recommended — works with any SMTP)
- [ ] Install MailKit NuGet package and add SMTP settings to `appsettings.json`
- [ ] Add `SendBookingConfirmationAsync` and `SendBookingCancellationAsync` to `IEmailService`
- [ ] Implement both methods in `EmailService` with HTML email bodies
- [ ] Inject `IEmailService` into `InspectionBookingService` and call fire-and-forget after successful `CreateAsync` and `CancelAsync`

> Email failures should log the error but not fail the booking (fire-and-forget). If reliable delivery is needed later, add a background job queue.
