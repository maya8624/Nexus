using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace Nexus.Application.Services
{
    public class DepositService : IDepositService
    {
        private readonly IDepositRepository _depositRepository;
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IListingRepository _listingRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<DepositService> _logger;
        private readonly StripeSettings _stripeSettings;
        private readonly SessionService _sessionService;
        private readonly IUserContext _userContext;

        public DepositService(
            IDepositRepository depositRepository,
            IUnitOfWork uow,
            IEmailService emailService,
            IListingRepository listingRepository,
            IPropertyRepository propertyRepository,
            ILogger<DepositService> logger,
            IOptions<StripeSettings> stripeSettings,
            SessionService sessionService,
            IUserContext userContext)
        {
            _depositRepository = depositRepository;
            _uow = uow;
            _emailService = emailService;
            _logger = logger;
            _stripeSettings = stripeSettings.Value;
            _sessionService = sessionService;
            _userContext = userContext;
            _listingRepository = listingRepository;
            _propertyRepository = propertyRepository;
        }

        public async Task<Result<DepositResponse>> CreateCheckoutSessionAsync(CreateDepositRequest request, CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var existingDeposit = await _depositRepository.GetByClientIdempotencyKeyAsync(userId, request.IdempotencyKey, ct);
            if (existingDeposit?.StripeSessionUrl != null)
                return Result<DepositResponse>.Success(MapDeposit(existingDeposit));

            var property = await _propertyRepository.GetByIdAsync(request.PropertyId, ct);
            if (property == null)
                return Result<DepositResponse>.NotFound("PropertyNotFound", "Property was not found.");

            var listing = await _listingRepository.GetByTypeAsync(ListingType.Rent, request.ListingId, ct);
            if (listing == null || listing.IsPublished == false)
                return Result<DepositResponse>.NotFound("ListingNotFound", "Active published listing was not found.");

            var deposit = existingDeposit ?? new Deposit
            {
                Id = Guid.NewGuid(),
                PropertyId = request.PropertyId,
                UserId = userId,
                ListingId = request.ListingId,
                Amount = request.Amount,
                Currency = Currency.AUD,
                Status = DepositStatus.Pending,
                IdempotencyKey = request.IdempotencyKey,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            if (existingDeposit == null)
            {
                await _depositRepository.Create(deposit, ct);
                await _uow.SaveChanges();
            }

            SessionCreateOptions sessionOptions = CreateSessionOptions(property, deposit);

            var session = await _sessionService.CreateAsync(
                sessionOptions,
                new RequestOptions { IdempotencyKey = $"checkout-session-deposit-{deposit.Id}" },
                ct);

            deposit.StripeSessionId = session.Id;
            deposit.StripeSessionUrl = session.Url;

            _depositRepository.Update(deposit);
            await _uow.SaveChanges();

            return Result<DepositResponse>.Success(MapDeposit(deposit));
        }

        private SessionCreateOptions CreateSessionOptions(ReadModels.PropertyReadModel property, Deposit deposit)
        {
            return new SessionCreateOptions
            {
                PaymentMethodTypes = ["card"],
                LineItems =
                            [
                                new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "aud",
                            UnitAmount = (long)Math.Round(deposit.Amount * 100, MidpointRounding.AwayFromZero),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Holding Deposit – {property.AddressLine1}",
                                Description = $"Tenant: {_userContext.Email}"
                            }
                        },
                        Quantity = 1
                    }
                            ],
                Mode = "payment",
                CustomerEmail = _userContext.Email,
                SuccessUrl = _stripeSettings.SuccessUrl,
                CancelUrl = _stripeSettings.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["deposit_id"] = deposit.Id.ToString()
                }
            };
        }


        public async Task FulfillDepositAsync(string stripeSessionId, CancellationToken ct)
        {
            var deposit = await _depositRepository.GetByStripeSessionIdAsync(stripeSessionId, ct);
            if (deposit == null)
            {
                _logger.LogWarning("FulfillDeposit: no deposit found for Stripe session {StripeSessionId}", stripeSessionId);
                return;
            }

            if (deposit.Status == DepositStatus.Paid)
                return;

            var session = await _sessionService.GetAsync(stripeSessionId, cancellationToken: ct);

            deposit.Status = DepositStatus.Paid;
            deposit.StripePaymentIntentId = session.PaymentIntentId;
            deposit.PaidAtUtc = DateTimeOffset.UtcNow;

            _depositRepository.Update(deposit);
            await _uow.SaveChanges();

            await _emailService.SendDepositConfirmationAsync(deposit, ct);
        }

        private static DepositResponse MapDeposit(Deposit deposit)
        {
            return new DepositResponse
            {
                Id = deposit.Id,
                UserId = deposit.UserId,
                PropertyId = deposit.PropertyId,
                ListingId = deposit.ListingId,
                Amount = deposit.Amount,
                Currency = deposit.Currency.ToString(),
                StripeSessionId = deposit.StripeSessionId,
                Status = deposit.Status.ToString(),
                PaidAtUtc = deposit.PaidAtUtc,
                SessionUrl = deposit.StripeSessionUrl
            };
        }
    }
}
