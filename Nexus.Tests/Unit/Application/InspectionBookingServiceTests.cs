using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Xunit;

namespace Nexus.Tests.Application
{
    public class InspectionBookingServiceTests
    {
        private readonly Mock<IInspectionBookingRepository> _bookingRepositoryMock = new();
        private readonly Mock<IInspectionSlotRepository> _slotRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUserContext> _userContextMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly IInspectionBookingService _service;
        private readonly Guid _userId = Guid.NewGuid();

        public InspectionBookingServiceTests()
        {
            _userContextMock.Setup(x => x.UserId).Returns(_userId.ToString());

            _service = new InspectionBookingService(
                _bookingRepositoryMock.Object,
                _slotRepositoryMock.Object,
                _userRepositoryMock.Object,
                _userContextMock.Object,
                _uowMock.Object);
        }

        [Fact]
        public async Task CreateBooking_WithMissingUser_ShouldReturnNotFound()
        {
            var request = new CreateInspectionBookingRequest { InspectionSlotId = Guid.NewGuid() };

            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _service.CreateAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("UserNotFound", Assert.Single(result.Errors).Code);
            _slotRepositoryMock.Verify(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _bookingRepositoryMock.Verify(x => x.Create(It.IsAny<InspectionBooking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_WithMissingSlot_ShouldReturnNotFound()
        {
            var request = new CreateInspectionBookingRequest { InspectionSlotId = Guid.NewGuid() };

            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _slotRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(request.InspectionSlotId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((InspectionSlot?)null);

            var result = await _service.CreateAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("SlotNotFound", Assert.Single(result.Errors).Code);
            _bookingRepositoryMock.Verify(x => x.Create(It.IsAny<InspectionBooking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_WithClosedSlot_ShouldReturnConflict()
        {
            var request = new CreateInspectionBookingRequest { InspectionSlotId = Guid.NewGuid() };
            var slot = BuildSlot(status: InspectionSlotStatus.Closed);

            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _slotRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(request.InspectionSlotId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(slot);

            var result = await _service.CreateAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("SlotNotAvailable", Assert.Single(result.Errors).Code);
            _bookingRepositoryMock.Verify(x => x.GetConfirmedCountForSlotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_WithFullSlot_ShouldReturnConflict()
        {
            var request = new CreateInspectionBookingRequest { InspectionSlotId = Guid.NewGuid() };
            var slot = BuildSlot(id: request.InspectionSlotId, capacity: 2);

            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _slotRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(request.InspectionSlotId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(slot);
            _bookingRepositoryMock
                .Setup(x => x.GetConfirmedCountForSlotAsync(slot.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            var result = await _service.CreateAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("SlotFull", Assert.Single(result.Errors).Code);
            _bookingRepositoryMock.Verify(x => x.HasActiveBookingForSlotAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_WithExistingActiveBooking_ShouldReturnConflict()
        {
            var request = new CreateInspectionBookingRequest { InspectionSlotId = Guid.NewGuid() };
            var slot = BuildSlot(id: request.InspectionSlotId);

            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _slotRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(request.InspectionSlotId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(slot);
            _bookingRepositoryMock
                .Setup(x => x.GetConfirmedCountForSlotAsync(slot.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _bookingRepositoryMock
                .Setup(x => x.HasActiveBookingForSlotAsync(slot.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _service.CreateAsync(request, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("DuplicateBooking", Assert.Single(result.Errors).Code);
            _bookingRepositoryMock.Verify(x => x.Create(It.IsAny<InspectionBooking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_WithValidInput_ShouldReturnSuccess()
        {
            var request = new CreateInspectionBookingRequest
            {
                InspectionSlotId = Guid.NewGuid(),
                Notes = "  Please call on arrival.  "
            };
            var slot = BuildSlot(id: request.InspectionSlotId);

            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _slotRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(request.InspectionSlotId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(slot);
            _bookingRepositoryMock
                .Setup(x => x.GetConfirmedCountForSlotAsync(slot.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _bookingRepositoryMock
                .Setup(x => x.HasActiveBookingForSlotAsync(slot.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _bookingRepositoryMock
                .Setup(x => x.Create(It.IsAny<InspectionBooking>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _uowMock
                .Setup(x => x.SaveChanges())
                .ReturnsAsync(1);

            var result = await _service.CreateAsync(request, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(slot.Id, result.Value!.InspectionSlotId);
            Assert.Equal(_userId, result.Value.UserId);
            Assert.Equal("Pending", result.Value.Status);
            Assert.Equal("Please call on arrival.", result.Value.Notes);

            _bookingRepositoryMock.Verify(x => x.Create(It.Is<InspectionBooking>(b =>
                b.UserId == _userId &&
                b.InspectionSlotId == slot.Id &&
                b.PropertyId == slot.PropertyId &&
                b.ListingId == slot.ListingId &&
                b.AgentId == slot.AgentId &&
                b.Status == InspectionBookingStatus.Pending &&
                b.Notes == "Please call on arrival." &&
                b.Id != Guid.Empty), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task GetMyBookings_WithExistingBookings_ShouldReturnMappedResults()
        {
            var bookings = new List<InspectionBooking>
            {
                BuildBooking(id: Guid.NewGuid(), userId: _userId, status: InspectionBookingStatus.Pending, notes: "First"),
                BuildBooking(id: Guid.NewGuid(), userId: _userId, status: InspectionBookingStatus.Confirmed, notes: "Second")
            };

            _bookingRepositoryMock
                .Setup(x => x.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(bookings);

            var result = await _service.GetMyBookingsAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value!.Count);
            Assert.Equal(bookings[0].Id, result.Value[0].Id);
            Assert.Equal("Pending", result.Value[0].Status);
            Assert.Equal("Confirmed", result.Value[1].Status);
        }

        [Fact]
        public async Task GetBookingById_WithMissingBooking_ShouldReturnNotFound()
        {
            var bookingId = Guid.NewGuid();

            _bookingRepositoryMock
                .Setup(x => x.GetByIdAsync(bookingId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((InspectionBooking?)null);

            var result = await _service.GetByIdAsync(bookingId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("BookingNotFound", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public async Task GetBookingById_WithExistingBooking_ShouldReturnMappedResult()
        {
            var booking = BuildBooking(id: Guid.NewGuid(), userId: _userId, status: InspectionBookingStatus.Confirmed, notes: "Window side");

            _bookingRepositoryMock
                .Setup(x => x.GetByIdAsync(booking.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            var result = await _service.GetByIdAsync(booking.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(booking.Id, result.Value!.Id);
            Assert.Equal("Confirmed", result.Value.Status);
            Assert.Equal("Window side", result.Value.Notes);
        }

        [Fact]
        public async Task CancelBooking_WithMissingBooking_ShouldReturnNotFound()
        {
            var bookingId = Guid.NewGuid();

            _bookingRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(bookingId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((InspectionBooking?)null);

            var result = await _service.CancelAsync(bookingId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("BookingNotFound", Assert.Single(result.Errors).Code);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CancelBooking_WithNonCancellableStatus_ShouldReturnConflict()
        {
            var booking = BuildBooking(id: Guid.NewGuid(), userId: _userId, status: InspectionBookingStatus.Completed);

            _bookingRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(booking.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            var result = await _service.CancelAsync(booking.Id, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("InvalidStatus", Assert.Single(result.Errors).Code);
            Assert.Equal(InspectionBookingStatus.Completed, booking.Status);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CancelBooking_WithPendingBooking_ShouldReturnSuccess()
        {
            var booking = BuildBooking(id: Guid.NewGuid(), userId: _userId, status: InspectionBookingStatus.Pending);

            _bookingRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(booking.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);
            _uowMock
                .Setup(x => x.SaveChanges())
                .ReturnsAsync(1);

            var beforeUpdate = booking.UpdatedAtUtc;
            var result = await _service.CancelAsync(booking.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Cancelled", result.Value!.Status);
            Assert.Equal(InspectionBookingStatus.Cancelled, booking.Status);
            Assert.True(booking.UpdatedAtUtc >= beforeUpdate);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        private static InspectionSlot BuildSlot(
            Guid? id = null,
            Guid? propertyId = null,
            Guid? listingId = null,
            Guid? agentId = null,
            int capacity = 3,
            InspectionSlotStatus status = InspectionSlotStatus.Open)
        {
            return new InspectionSlot
            {
                Id = id ?? Guid.NewGuid(),
                PropertyId = propertyId ?? Guid.NewGuid(),
                ListingId = listingId ?? Guid.NewGuid(),
                AgentId = agentId ?? Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                StartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                EndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                Capacity = capacity,
                Status = status,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
        }

        private static InspectionBooking BuildBooking(
            Guid? id = null,
            Guid? userId = null,
            InspectionBookingStatus status = InspectionBookingStatus.Pending,
            string? notes = null)
        {
            return new InspectionBooking
            {
                Id = id ?? Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid(),
                InspectionSlotId = Guid.NewGuid(),
                PropertyId = Guid.NewGuid(),
                ListingId = Guid.NewGuid(),
                AgentId = Guid.NewGuid(),
                Status = status,
                Notes = notes,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                UpdatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1)
            };
        }
    }
}
