using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class InspectionBookingService : IInspectionBookingService
    {
        private readonly IInspectionBookingRepository _bookingRepository;
        private readonly IInspectionSlotRepository _slotRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserContext _userContext;
        private readonly IUnitOfWork _uow;

        public InspectionBookingService(
            IInspectionBookingRepository bookingRepository,
            IInspectionSlotRepository slotRepository,
            IUserRepository userRepository,
            IUserContext userContext,
            IUnitOfWork uow)
        {
            _bookingRepository = bookingRepository;
            _slotRepository = slotRepository;
            _userRepository = userRepository;
            _userContext = userContext;
            _uow = uow;
        }

        public async Task<Result<InspectionBookingDto>> CreateAsync(CreateInspectionBookingRequest request, CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (!userExists)
                return Result<InspectionBookingDto>.NotFound("UserNotFound", "User not found or inactive.");

            var slot = await _slotRepository.GetByIdForUpdateAsync(request.InspectionSlotId, ct);
            if (slot is null || slot.Status != InspectionSlotStatus.Open)
                return Result<InspectionBookingDto>.NotFound("SlotNotFound", "Inspection slot not found or not available.");

            var confirmedCount = await _bookingRepository.GetConfirmedCountForSlotAsync(slot.Id, ct);
            if (confirmedCount >= slot.Capacity)
                return Result<InspectionBookingDto>.Conflict("SlotFull", "This inspection slot is fully booked.");

            var hasActiveBooking = await _bookingRepository.HasActiveBookingForSlotAsync(slot.Id, userId, ct);
            if (hasActiveBooking)
                return Result<InspectionBookingDto>.Conflict("DuplicateBooking", "You already have an active booking for this slot.");

            var now = DateTimeOffset.UtcNow;
            var booking = new InspectionBooking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InspectionSlotId = slot.Id,
                PropertyId = slot.PropertyId,
                ListingId = slot.ListingId,
                AgentId = slot.AgentId,
                Status = InspectionBookingStatus.Pending,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _bookingRepository.Create(booking, ct);
            await _uow.SaveChanges();

            return Result<InspectionBookingDto>.Success(MapToDto(booking));
        }

        public async Task<Result<IReadOnlyList<InspectionBookingDto>>> GetMyBookingsAsync(CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var bookings = await _bookingRepository.GetByUserIdAsync(userId, ct);
            var dtos = bookings.Select(MapToDto).ToList();

            return Result<IReadOnlyList<InspectionBookingDto>>.Success(dtos);
        }

        public async Task<Result<InspectionBookingDto>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var booking = await _bookingRepository.GetByIdAsync(id, userId, ct);
            if (booking is null)
                return Result<InspectionBookingDto>.NotFound("BookingNotFound", "Booking not found.");

            return Result<InspectionBookingDto>.Success(MapToDto(booking));
        }

        public async Task<Result<InspectionBookingDto>> CancelAsync(Guid id, CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var booking = await _bookingRepository.GetByIdForUpdateAsync(id, userId, ct);
            if (booking is null)
                return Result<InspectionBookingDto>.NotFound("BookingNotFound", "Booking not found.");

            if (booking.Status != InspectionBookingStatus.Pending && booking.Status != InspectionBookingStatus.Confirmed)
                return Result<InspectionBookingDto>.Conflict("InvalidStatus", "Only pending or confirmed bookings can be cancelled.");

            booking.Status = InspectionBookingStatus.Cancelled;
            booking.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _uow.SaveChanges();

            return Result<InspectionBookingDto>.Success(MapToDto(booking));
        }

        private static InspectionBookingDto MapToDto(InspectionBooking booking) => new()
        {
            Id = booking.Id,
            UserId = booking.UserId,
            InspectionSlotId = booking.InspectionSlotId,
            PropertyId = booking.PropertyId,
            ListingId = booking.ListingId,
            AgentId = booking.AgentId,
            Status = booking.Status.ToString(),
            Notes = booking.Notes,
            CreatedAtUtc = booking.CreatedAtUtc,
            UpdatedAtUtc = booking.UpdatedAtUtc
        };
    }
}
