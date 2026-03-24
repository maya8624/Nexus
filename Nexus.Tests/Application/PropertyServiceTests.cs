using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Interfaces;
using Xunit;

namespace Nexus.Tests.Application
{
    public class PropertyServiceTests
    {
        private readonly Mock<IPropertyRepository> _propertyRepository = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly PropertyService _service;

        public PropertyServiceTests()
        {
            _service = new PropertyService(
                _propertyRepository.Object,
                new PropertyQueryRequestValidator(),
                new CreateInspectionBookingRequestValidator(),
                new CheckInspectionAvailabilityRequestValidator(),
                _uow.Object);
        }

        [Fact]
        public async Task CreateInspectionBookingAsync_Should_ReturnSuccess_When_RequestIsValid()
        {
            var request = CreateValidRequest();

            _propertyRepository.Setup(x => x.UserExists(request.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _propertyRepository.Setup(x => x.GetBookingContext(request.PropertyId, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateBookingContext(request.PropertyId, request.ListingId, request.AgentId));
            _propertyRepository.Setup(x => x.HasDuplicateBooking(
                    request.UserId,
                    request.PropertyId,
                    request.ListingId,
                    request.InspectionStartAtUtc,
                    request.InspectionEndAtUtc,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _propertyRepository.Setup(x => x.HasOverlappingConfirmedBooking(
                    request.PropertyId,
                    request.InspectionStartAtUtc,
                    request.InspectionEndAtUtc,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _propertyRepository.Setup(x => x.AgentExists(request.AgentId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _service.CreateInspectionBookingAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Pending", result.Value!.Status);
            _propertyRepository.Verify(x => x.AddInspectionBooking(It.IsAny<InspectionBooking>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task CreateInspectionBookingAsync_Should_ReturnNotFound_When_PropertyDoesNotExist()
        {
            var request = CreateValidRequest();

            _propertyRepository.Setup(x => x.UserExists(request.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _propertyRepository.Setup(x => x.GetBookingContext(request.PropertyId, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BookingContextReadModel?)null);

            var result = await _service.CreateInspectionBookingAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task CreateInspectionBookingAsync_Should_ReturnConflict_When_ListingIsInvalid()
        {
            var request = CreateValidRequest();

            _propertyRepository.Setup(x => x.UserExists(request.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _propertyRepository.Setup(x => x.GetBookingContext(request.PropertyId, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BookingContextReadModel
                {
                    PropertyId = request.PropertyId,
                    PropertyIsActive = true,
                    ListingId = request.ListingId,
                    ListingIsPublished = false,
                    ListingStatus = ListingStatus.Active.ToString(),
                    ListingAgentId = request.AgentId
                });

            var result = await _service.CreateInspectionBookingAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
        }

        [Fact]
        public async Task CreateInspectionBookingAsync_Should_ReturnValidationError_When_TimeRangeIsInvalid()
        {
            var request = CreateValidRequest();
            request = new CreateInspectionBookingRequest
            {
                UserId = request.UserId,
                PropertyId = request.PropertyId,
                ListingId = request.ListingId,
                AgentId = request.AgentId,
                InspectionStartAtUtc = DateTimeOffset.UtcNow.AddHours(2),
                InspectionEndAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                Notes = request.Notes
            };

            var result = await _service.CreateInspectionBookingAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.ValidationError, result.Status);
        }

        [Fact]
        public async Task CreateInspectionBookingAsync_Should_ReturnValidationError_When_RequestIsInThePast()
        {
            var request = CreateValidRequest();
            request = new CreateInspectionBookingRequest
            {
                UserId = request.UserId,
                PropertyId = request.PropertyId,
                ListingId = request.ListingId,
                AgentId = request.AgentId,
                InspectionStartAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
                InspectionEndAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                Notes = request.Notes
            };

            var result = await _service.CreateInspectionBookingAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.ValidationError, result.Status);
        }

        [Fact]
        public async Task CreateInspectionBookingAsync_Should_ReturnConflict_When_RequestIsDuplicate()
        {
            var request = CreateValidRequest();

            _propertyRepository.Setup(x => x.UserExists(request.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _propertyRepository.Setup(x => x.GetBookingContext(request.PropertyId, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateBookingContext(request.PropertyId, request.ListingId, request.AgentId));
            _propertyRepository.Setup(x => x.AgentExists(request.AgentId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _propertyRepository.Setup(x => x.HasDuplicateBooking(
                    request.UserId,
                    request.PropertyId,
                    request.ListingId,
                    request.InspectionStartAtUtc,
                    request.InspectionEndAtUtc,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _service.CreateInspectionBookingAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
        }

        [Fact]
        public async Task CheckInspectionAvailabilityAsync_Should_ReturnUnavailable_When_ConfirmedBookingOverlaps()
        {
            var request = CreateAvailabilityRequest();

            _propertyRepository.Setup(x => x.GetBookingContext(request.PropertyId, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateBookingContext(request.PropertyId, request.ListingId, null));
            _propertyRepository.Setup(x => x.HasOverlappingConfirmedBooking(
                    request.PropertyId,
                    request.InspectionStartAtUtc,
                    request.InspectionEndAtUtc,
                    request.ExcludeBookingId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _service.CheckInspectionAvailabilityAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.False(result.Value!.IsAvailable);
        }

        [Fact]
        public async Task CheckInspectionAvailabilityAsync_Should_IgnorePendingBookings_When_NoConfirmedBookingOverlaps()
        {
            var request = CreateAvailabilityRequest();

            _propertyRepository.Setup(x => x.GetBookingContext(request.PropertyId, request.ListingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateBookingContext(request.PropertyId, request.ListingId, null));
            _propertyRepository.Setup(x => x.HasOverlappingConfirmedBooking(
                    request.PropertyId,
                    request.InspectionStartAtUtc,
                    request.InspectionEndAtUtc,
                    request.ExcludeBookingId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _service.CheckInspectionAvailabilityAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.IsAvailable);
        }

        [Fact]
        public async Task CancelInspectionBookingAsync_Should_ReturnSuccess_When_StatusIsPending()
        {
            var booking = CreateBooking(InspectionBookingStatus.Pending);

            _propertyRepository.Setup(x => x.GetInspectionBookingForUpdate(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

            var result = await _service.CancelInspectionBookingAsync(booking.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Cancelled", result.Value!.Status);
            _uow.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task CancelInspectionBookingAsync_Should_ReturnConflict_When_StatusDoesNotAllowCancellation()
        {
            var booking = CreateBooking(InspectionBookingStatus.Cancelled);

            _propertyRepository.Setup(x => x.GetInspectionBookingForUpdate(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

            var result = await _service.CancelInspectionBookingAsync(booking.Id, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
        }

        private static CreateInspectionBookingRequest CreateValidRequest()
        {
            return new CreateInspectionBookingRequest
            {
                UserId = Guid.NewGuid(),
                PropertyId = Guid.NewGuid(),
                ListingId = Guid.NewGuid(),
                AgentId = Guid.NewGuid(),
                InspectionStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                InspectionEndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                Notes = "Morning inspection preferred."
            };
        }

        private static CheckInspectionAvailabilityRequest CreateAvailabilityRequest()
        {
            return new CheckInspectionAvailabilityRequest
            {
                PropertyId = Guid.NewGuid(),
                ListingId = Guid.NewGuid(),
                InspectionStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                InspectionEndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
            };
        }

        private static BookingContextReadModel CreateBookingContext(Guid propertyId, Guid listingId, Guid? agentId)
        {
            return new BookingContextReadModel
            {
                PropertyId = propertyId,
                PropertyIsActive = true,
                ListingId = listingId,
                ListingIsPublished = true,
                ListingStatus = ListingStatus.Active.ToString(),
                ListingAgentId = agentId
            };
        }

        private static InspectionBooking CreateBooking(InspectionBookingStatus status)
        {
            return new InspectionBooking
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                PropertyId = Guid.NewGuid(),
                ListingId = Guid.NewGuid(),
                InspectionStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                InspectionEndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                Status = status,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }
}
