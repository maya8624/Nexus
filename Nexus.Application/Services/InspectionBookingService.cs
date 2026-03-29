using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Services
{
    public class InspectionBookingService : IInspectionBookingService
    {

        private readonly IAgentRepository _agentRepository;
        private readonly IInspectionBookingRepository _bookingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _uow;

        public InspectionBookingService(
            IAgentRepository agentRepository,
            IInspectionBookingRepository bookingRepository,
            IUserRepository userRepository,
            IUnitOfWork uow)
        {
            _agentRepository = agentRepository;
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
            _uow = uow;
        }


        public async Task<Result<InspectionBookingDto>> CreateInspectionBookingAsync(CreateInspectionBookingRequest request, CancellationToken ct)
        {
            var userExists = await _userRepository.IsAny(x => x.Id == request.UserId && x.IsActive, ct);
            if (userExists == false)
            {
                return Result<InspectionBookingDto>.NotFound("UserNotFound", "The specified user was not found.");
            }

            var hasDuplicateBooking = await _bookingRepository.HasDuplicateBooking(request, ct);
            if (hasDuplicateBooking)
            {
                return Result<InspectionBookingDto>.Conflict("DuplicateBooking", "A booking request already exists for the same user, property, listing, and time window.");
            }

            var hasConfirmedConflict = await _bookingRepository.HasOverlappingConfirmedBooking(request, ct);
            if (hasConfirmedConflict)
            {
                return Result<InspectionBookingDto>.Conflict("BookingConflict", "The requested inspection time overlaps an existing confirmed booking.");
            }

            var agentExists = await _agentRepository.IsAny(x => x.Id == request.AgentId && x.IsActive, ct);
            if (agentExists == false)
            {
                return Result<InspectionBookingDto>.NotFound("AgentNotFound", "The specified agent was not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var booking = new InspectionBooking
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                PropertyId = request.PropertyId,
                ListingId = request.ListingId,
                AgentId = request.AgentId,
                InspectionStartAtUtc = request.InspectionStartAtUtc,
                InspectionEndAtUtc = request.InspectionEndAtUtc,
                Status = InspectionBookingStatus.Pending,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _bookingRepository.Create(booking, ct);
            await _uow.SaveChanges();

            return Result<InspectionBookingDto>.Success(MapInspectionBookingDto(booking));
        }

        public async Task<Result<InspectionBookingDto>> CancelInspectionBookingAsync(Guid id, CancellationToken ct)
        {
            //TODO: get the current user from auth
            var booking = await _bookingRepository.GetInspectionBookingForUpdate(id, ct);
            if (booking == null)
            {
                return Result<InspectionBookingDto>.NotFound("BookingNotFound", "The specified booking was not found.");
            }

            if (booking.Status != InspectionBookingStatus.Pending &&
                booking.Status != InspectionBookingStatus.Confirmed)
            {
                return Result<InspectionBookingDto>.Conflict("InvalidBookingStatus", "Only pending or confirmed bookings can be cancelled.");
            }

            booking.Status = InspectionBookingStatus.Cancelled;
            booking.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _uow.SaveChanges();

            return Result<InspectionBookingDto>.Success(MapInspectionBookingDto(booking));
        }

        public async Task<Result<InspectionBookingDto>> GetInspectionBookingByIdAsync(Guid id, CancellationToken ct)
        {
            var booking = await _bookingRepository.GetInspectionBookingById(id, ct);

            if (booking == null)
                return Result<InspectionBookingDto>.NotFound("BookingNotFound", "The specified booking was not found.");

            return Result<InspectionBookingDto>.Success(MapInspectionBookingDto(booking));
        }

        //public async Task<Result<InspectionAvailabilityResponse>> CheckInspectionAvailabilityAsync(CheckInspectionAvailabilityRequest request, CancellationToken ct)
        //{
        //    var bookingContext = await _propertyRepository.GetBookingContext(request.PropertyId, request.ListingId, ct);
        //    if (bookingContext == null || bookingContext.PropertyIsActive == false)
        //    {
        //        return Result<InspectionAvailabilityResponse>.NotFound("PropertyNotFound", "The specified property was not found or is inactive.");
        //    }

        //    if (bookingContext.ListingIsPublished == false || IsListingActive(bookingContext.ListingStatus) == false)
        //    {
        //        return Result<InspectionAvailabilityResponse>.Conflict("ListingUnavailable", "The specified listing is not available for inspection booking.");
        //    }

        //    var hasConfirmedConflict = await _propertyRepository.HasOverlappingConfirmedBooking(request, ct);

        //    return Result<InspectionAvailabilityResponse>.Success(new InspectionAvailabilityResponse
        //    {
        //        IsAvailable = hasConfirmedConflict == false,
        //        Message = hasConfirmedConflict
        //            ? "The requested inspection time conflicts with an existing confirmed booking."
        //            : "The requested inspection time is available."
        //    });
        //}

        private static InspectionBookingDto MapInspectionBookingDto(InspectionBooking booking)
        {
            return new InspectionBookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                PropertyId = booking.PropertyId,
                ListingId = booking.ListingId ?? Guid.Empty,
                AgentId = booking.AgentId,
                InspectionStartAtUtc = booking.InspectionStartAtUtc,
                InspectionEndAtUtc = booking.InspectionEndAtUtc ?? booking.InspectionStartAtUtc,
                Status = booking.Status.ToString(),
                Notes = booking.Notes,
                CreatedAtUtc = booking.CreatedAtUtc,
                UpdatedAtUtc = booking.UpdatedAtUtc
            };
        }

        public Task<Result<InspectionAvailabilityResponse>> CheckInspectionAvailabilityAsync(CheckInspectionAvailabilityRequest request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
