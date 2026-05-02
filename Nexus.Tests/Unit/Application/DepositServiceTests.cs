using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Tests.Unit.Helpers;
using Stripe;
using Stripe.Checkout;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class DepositServiceTests
    {
        private const string TenantEmail = "tenant@example.com";
        private const string SuccessUrl = "https://example.com/success";
        private const string CancelUrl = "https://example.com/cancel";

        private readonly Mock<IDepositRepository> _depositRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IListingRepository> _listingRepositoryMock = new();
        private readonly Mock<IPropertyRepository> _propertyRepositoryMock = new();
        private readonly Mock<ILogger<DepositService>> _loggerMock = new();
        private readonly Mock<IUserContext> _userContextMock = new();
        private readonly Mock<SessionService> _sessionServiceMock = new();

        private readonly Guid _userId = Guid.NewGuid();
        private readonly DepositService _service;

        public DepositServiceTests()
        {
            _userContextMock.Setup(x => x.UserId).Returns(_userId.ToString());
            _userContextMock.Setup(x => x.Email).Returns(TenantEmail);
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);

            var stripeSettings = Options.Create(new StripeSettings
            {
                SuccessUrl = SuccessUrl,
                CancelUrl = CancelUrl
            });

            _service = new DepositService(
                _depositRepositoryMock.Object,
                _uowMock.Object,
                _emailServiceMock.Object,
                _listingRepositoryMock.Object,
                _propertyRepositoryMock.Object,
                _loggerMock.Object,
                stripeSettings,
                _sessionServiceMock.Object,
                _userContextMock.Object);
        }

        [Fact]
        public async Task CreateCheckoutSession_IdempotencyHit_WithSessionUrl_ReturnsSuccessImmediately()
        {
            var request = BuildRequest();
            var existing = BuildDeposit(request, stripeSessionUrl: "https://checkout.stripe.com/existing");

            _depositRepositoryMock
                .Setup(x => x.GetByClientIdempotencyKeyAsync(_userId, request.IdempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var result = await _service.CreateCheckoutSessionAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(existing.Id, result.Value!.Id);
            Assert.Equal("https://checkout.stripe.com/existing", result.Value.SessionUrl);

            _propertyRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _sessionServiceMock.Verify(x => x.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateCheckoutSession_IdempotencyHit_WithoutSessionUrl_ResumesStripeSession()
        {
            var request = BuildRequest();
            var orphaned = BuildDeposit(request, stripeSessionUrl: null);
            var property = BuildProperty(request.PropertyId);
            var listing = BuildListing(request.ListingId);

            SetupRequestLookup(request, orphaned, property, listing);
            var stripeCall = SetupStripeCreate("cs_resumed", "https://checkout.stripe.com/resumed");

            var result = await _service.CreateCheckoutSessionAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("https://checkout.stripe.com/resumed", result.Value!.SessionUrl);
            Assert.Equal("cs_resumed", orphaned.StripeSessionId);
            Assert.Equal("https://checkout.stripe.com/resumed", orphaned.StripeSessionUrl);
            AssertStripeSessionOptions(stripeCall.Options!, orphaned, property, request.Amount);
            Assert.Equal($"checkout-session-deposit-{orphaned.Id}", stripeCall.RequestOptions!.IdempotencyKey);

            _depositRepositoryMock.Verify(x => x.Create(It.IsAny<Deposit>(), It.IsAny<CancellationToken>()), Times.Never);
            _depositRepositoryMock.Verify(x => x.Update(orphaned), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task CreateCheckoutSession_PropertyNotFound_ReturnsNotFound()
        {
            var request = BuildRequest();

            _depositRepositoryMock
                .Setup(x => x.GetByClientIdempotencyKeyAsync(_userId, request.IdempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Deposit?)null);
            _propertyRepositoryMock
                .Setup(x => x.GetByIdAsync(request.PropertyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PropertyReadModel?)null);

            var result = await _service.CreateCheckoutSessionAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("PropertyNotFound", Assert.Single(result.Errors).Code);
            _sessionServiceMock.Verify(x => x.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateCheckoutSession_ListingNotFound_ReturnsNotFound()
        {
            var request = BuildRequest();
            var property = BuildProperty(request.PropertyId);

            _depositRepositoryMock
                .Setup(x => x.GetByClientIdempotencyKeyAsync(_userId, request.IdempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Deposit?)null);
            _propertyRepositoryMock
                .Setup(x => x.GetByIdAsync(request.PropertyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(property);
            _listingRepositoryMock
                .Setup(x => x.GetByTypeAsync(ListingType.Rent, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Listing?)null);

            var result = await _service.CreateCheckoutSessionAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("ListingNotFound", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public async Task CreateCheckoutSession_ListingNotPublished_ReturnsNotFound()
        {
            var request = BuildRequest();
            var property = BuildProperty(request.PropertyId);
            var unpublishedListing = BuildListing(request.ListingId, isPublished: false);

            _depositRepositoryMock
                .Setup(x => x.GetByClientIdempotencyKeyAsync(_userId, request.IdempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Deposit?)null);
            _propertyRepositoryMock
                .Setup(x => x.GetByIdAsync(request.PropertyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(property);
            _listingRepositoryMock
                .Setup(x => x.GetByTypeAsync(ListingType.Rent, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unpublishedListing);

            var result = await _service.CreateCheckoutSessionAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("ListingNotFound", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public async Task CreateCheckoutSession_NewDeposit_CreatesDepositAndStripeSession()
        {
            var request = BuildRequest();
            var property = BuildProperty(request.PropertyId);
            var listing = BuildListing(request.ListingId);

            SetupRequestLookup(request, existingDeposit: null, property, listing);

            Deposit? createdDeposit = null;
            _depositRepositoryMock
                .Setup(x => x.Create(It.IsAny<Deposit>(), It.IsAny<CancellationToken>()))
                .Callback<Deposit, CancellationToken>((deposit, _) => createdDeposit = deposit)
                .Returns(Task.CompletedTask);

            var stripeCall = SetupStripeCreate("cs_new", "https://checkout.stripe.com/new");

            var result = await _service.CreateCheckoutSessionAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("https://checkout.stripe.com/new", result.Value!.SessionUrl);
            Assert.Equal("cs_new", result.Value.StripeSessionId);

            Assert.NotNull(createdDeposit);
            Assert.Equal(_userId, createdDeposit!.UserId);
            Assert.Equal(request.Amount, createdDeposit.Amount);
            Assert.Equal(DepositStatus.Pending, createdDeposit.Status);
            Assert.Equal(Currency.AUD, createdDeposit.Currency);
            Assert.Equal(request.IdempotencyKey, createdDeposit.IdempotencyKey);
            Assert.Equal("cs_new", createdDeposit.StripeSessionId);
            Assert.Equal("https://checkout.stripe.com/new", createdDeposit.StripeSessionUrl);
            AssertStripeSessionOptions(stripeCall.Options!, createdDeposit, property, request.Amount);
            Assert.Equal($"checkout-session-deposit-{createdDeposit.Id}", stripeCall.RequestOptions!.IdempotencyKey);

            _depositRepositoryMock.Verify(x => x.Create(createdDeposit, It.IsAny<CancellationToken>()), Times.Once);
            _depositRepositoryMock.Verify(x => x.Update(createdDeposit), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateCheckoutSession_NewDeposit_WhenStripeCreateFails_PersistsInitialDepositOnly()
        {
            var request = BuildRequest();
            var property = BuildProperty(request.PropertyId);
            var listing = BuildListing(request.ListingId);

            SetupRequestLookup(request, existingDeposit: null, property, listing);

            Deposit? createdDeposit = null;
            _depositRepositoryMock
                .Setup(x => x.Create(It.IsAny<Deposit>(), It.IsAny<CancellationToken>()))
                .Callback<Deposit, CancellationToken>((deposit, _) => createdDeposit = deposit)
                .Returns(Task.CompletedTask);

            _sessionServiceMock
                .Setup(x => x.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new StripeException("stripe unavailable"));

            await Assert.ThrowsAsync<StripeException>(() => _service.CreateCheckoutSessionAsync(request, CancellationToken.None));

            Assert.NotNull(createdDeposit);
            Assert.Equal(string.Empty, createdDeposit!.StripeSessionId);
            Assert.Null(createdDeposit.StripeSessionUrl);

            _depositRepositoryMock.Verify(x => x.Create(createdDeposit, It.IsAny<CancellationToken>()), Times.Once);
            _depositRepositoryMock.Verify(x => x.Update(It.IsAny<Deposit>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task CreateCheckoutSession_ExistingDeposit_WhenStripeCreateFails_DoesNotUpdateDeposit()
        {
            var request = BuildRequest();
            var existingDeposit = BuildDeposit(request, stripeSessionUrl: null);
            var property = BuildProperty(request.PropertyId);
            var listing = BuildListing(request.ListingId);

            SetupRequestLookup(request, existingDeposit, property, listing);

            _sessionServiceMock
                .Setup(x => x.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new StripeException("stripe unavailable"));

            await Assert.ThrowsAsync<StripeException>(() => _service.CreateCheckoutSessionAsync(request, CancellationToken.None));

            Assert.Equal(string.Empty, existingDeposit.StripeSessionId);
            Assert.Null(existingDeposit.StripeSessionUrl);

            _depositRepositoryMock.Verify(x => x.Create(It.IsAny<Deposit>(), It.IsAny<CancellationToken>()), Times.Never);
            _depositRepositoryMock.Verify(x => x.Update(It.IsAny<Deposit>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task FulfillDeposit_DepositNotFound_LogsWarningAndDoesNotSave()
        {
            _depositRepositoryMock
                .Setup(x => x.GetByStripeSessionIdAsync("cs_missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Deposit?)null);

            await _service.FulfillDepositAsync("cs_missing", CancellationToken.None);

            _loggerMock.VerifyLog(LogLevel.Warning, "FulfillDeposit: no deposit found for Stripe session");
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
            _emailServiceMock.Verify(x => x.SendDepositConfirmationAsync(It.IsAny<Deposit>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FulfillDeposit_AlreadyPaid_DoesNotUpdateOrSendEmail()
        {
            var deposit = new Deposit { Id = Guid.NewGuid(), Status = DepositStatus.Paid };

            _depositRepositoryMock
                .Setup(x => x.GetByStripeSessionIdAsync("cs_paid", It.IsAny<CancellationToken>()))
                .ReturnsAsync(deposit);

            await _service.FulfillDepositAsync("cs_paid", CancellationToken.None);

            _sessionServiceMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<SessionGetOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
            _depositRepositoryMock.Verify(x => x.Update(It.IsAny<Deposit>()), Times.Never);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
            _emailServiceMock.Verify(x => x.SendDepositConfirmationAsync(It.IsAny<Deposit>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FulfillDeposit_HappyPath_MarksAsPaidAndSendsEmail()
        {
            var deposit = new Deposit { Id = Guid.NewGuid(), Status = DepositStatus.Pending };
            const string sessionId = "cs_complete";
            const string paymentIntentId = "pi_123";

            _depositRepositoryMock
                .Setup(x => x.GetByStripeSessionIdAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(deposit);
            _sessionServiceMock
                .Setup(x => x.GetAsync(sessionId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Session { Id = sessionId, PaymentIntentId = paymentIntentId });

            await _service.FulfillDepositAsync(sessionId, CancellationToken.None);

            Assert.Equal(DepositStatus.Paid, deposit.Status);
            Assert.Equal(paymentIntentId, deposit.StripePaymentIntentId);
            Assert.NotNull(deposit.PaidAtUtc);

            _depositRepositoryMock.Verify(x => x.Update(deposit), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
            _emailServiceMock.Verify(x => x.SendDepositConfirmationAsync(deposit, It.IsAny<CancellationToken>()), Times.Once);
        }

        private CreateDepositRequest BuildRequest() => new()
        {
            PropertyId = Guid.NewGuid(),
            ListingId = Guid.NewGuid(),
            Amount = 500m,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        private Deposit BuildDeposit(CreateDepositRequest request, string? stripeSessionUrl) => new()
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            PropertyId = request.PropertyId,
            ListingId = request.ListingId,
            Amount = request.Amount,
            Currency = Currency.AUD,
            Status = DepositStatus.Pending,
            IdempotencyKey = request.IdempotencyKey,
            StripeSessionUrl = stripeSessionUrl
        };

        private static PropertyReadModel BuildProperty(Guid id) => new()
        {
            Id = id,
            AddressLine1 = "1 Test St"
        };

        private static Listing BuildListing(Guid id, bool isPublished = true) => new()
        {
            Id = id,
            IsPublished = isPublished,
            ListingType = ListingType.Rent
        };

        private void SetupRequestLookup(CreateDepositRequest request, Deposit? existingDeposit, PropertyReadModel property, Listing listing)
        {
            _depositRepositoryMock
                .Setup(x => x.GetByClientIdempotencyKeyAsync(_userId, request.IdempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingDeposit);
            _propertyRepositoryMock
                .Setup(x => x.GetByIdAsync(request.PropertyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(property);
            _listingRepositoryMock
                .Setup(x => x.GetByTypeAsync(ListingType.Rent, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(listing);
        }

        private StripeCreateCall SetupStripeCreate(string sessionId, string sessionUrl)
        {
            var stripeCall = new StripeCreateCall();

            _sessionServiceMock
                .Setup(x => x.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Callback<SessionCreateOptions, RequestOptions, CancellationToken>((options, requestOptions, _) =>
                {
                    stripeCall.Options = options;
                    stripeCall.RequestOptions = requestOptions;
                })
                .ReturnsAsync(new Session { Id = sessionId, Url = sessionUrl });

            return stripeCall;
        }

        private static void AssertStripeSessionOptions(SessionCreateOptions options, Deposit deposit, PropertyReadModel property, decimal expectedAmount)
        {
            Assert.Equal("payment", options.Mode);
            Assert.Equal(TenantEmail, options.CustomerEmail);
            Assert.Equal(SuccessUrl, options.SuccessUrl);
            Assert.Equal(CancelUrl, options.CancelUrl);
            Assert.Equal("card", Assert.Single(options.PaymentMethodTypes));
            Assert.Equal(deposit.Id.ToString(), options.Metadata["deposit_id"]);

            var lineItem = Assert.Single(options.LineItems);
            Assert.Equal(1L, lineItem.Quantity);
            Assert.NotNull(lineItem.PriceData);
            Assert.Equal("aud", lineItem.PriceData!.Currency);
            Assert.Equal((long)Math.Round(expectedAmount * 100, MidpointRounding.AwayFromZero), lineItem.PriceData.UnitAmount);
            Assert.NotNull(lineItem.PriceData.ProductData);
            Assert.Contains(property.AddressLine1 ?? string.Empty, lineItem.PriceData.ProductData!.Name);
            Assert.Equal($"Tenant: {TenantEmail}", lineItem.PriceData.ProductData.Description);
        }

        private sealed class StripeCreateCall
        {
            public SessionCreateOptions? Options { get; set; }

            public RequestOptions? RequestOptions { get; set; }
        }
    }

}
