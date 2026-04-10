using Nexus.Application.Common;
using Nexus.Application.Constants;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class InspectionSlotService : IInspectionSlotService
    {
        private readonly IInspectionSlotRepository _slotRepository;
        private readonly IInspectionBookingRepository _bookingRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IUserContext _userContext;
        private readonly IUnitOfWork _uow;

        public InspectionSlotService(
            IInspectionSlotRepository slotRepository,
            IInspectionBookingRepository bookingRepository,
            IAgentRepository agentRepository,
            IPropertyRepository propertyRepository,
            IUserContext userContext,
            IUnitOfWork uow)
        {
            _slotRepository = slotRepository;
            _bookingRepository = bookingRepository;
            _agentRepository = agentRepository;
            _propertyRepository = propertyRepository;
            _userContext = userContext;
            _uow = uow;
        }

        public async Task<Result<InspectionSlotDto>> CreateAsync(InspectionSlotRequest request, CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var agentExists = await _agentRepository.IsAny(x => x.Id == request.AgentId && x.IsActive, ct);
            if (agentExists == false)
                return Result<InspectionSlotDto>.NotFound("AgentNotFound", "Agent not found or inactive.");

            var propertyExists = await _propertyRepository.IsAny(x => x.Id == request.PropertyId && x.IsActive, ct);
            if (propertyExists == false)
                return Result<InspectionSlotDto>.NotFound("PropertyNotFound", "Property not found or inactive.");

            var hasOverlap = await _slotRepository.HasConflictingSlotAsync(request, ct);
            if (hasOverlap)
                return Result<InspectionSlotDto>.Conflict("SlotOverlap", "Agent already has a slot that overlaps the requested time window for this property.");

            var now = DateTimeOffset.UtcNow;
            var slot = new InspectionSlot
            {
                Id = Guid.NewGuid(),
                PropertyId = request.PropertyId,
                ListingId = request.ListingId ?? Guid.Empty,
                AgentId = request.AgentId,
                UserId = userId,
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                Capacity = request.Capacity,
                Status = InspectionSlotStatus.Open,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _slotRepository.Create(slot, ct);
            await _uow.SaveChanges();

            return Result<InspectionSlotDto>.Success(MapToDto(slot));
        }

        public async Task<Result<InspectionSlotDto>> Update(Guid id, UpdateInspectionSlotRequest request, CancellationToken ct)
        {
            var slot = await _slotRepository.GetByIdForUpdateAsync(id, ct);
            if (slot is null)
                return Result<InspectionSlotDto>.NotFound("SlotNotFound", "Inspection slot not found.");

            if (slot.Status == InspectionSlotStatus.Cancelled)
                return Result<InspectionSlotDto>.Conflict("SlotCancelled", "A cancelled slot cannot be updated.");

            var agentExists = await _agentRepository.IsAny(x => x.Id == request.AgentId && x.IsActive, ct);
            if (agentExists == false)
                return Result<InspectionSlotDto>.NotFound("AgentNotFound", "Agent not found or inactive.");

            var inspectionSlotRequest = new InspectionSlotRequest
            {
                PropertyId = slot.PropertyId,
                AgentId = request.AgentId,
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                Capacity = request.Capacity,
                Notes = request.Notes
            };

            var hasOverlap = await _slotRepository.HasConflictingSlotAsync(inspectionSlotRequest, ct, excludeId: id);
            if (hasOverlap)
                return Result<InspectionSlotDto>.Conflict("SlotOverlap", "Agent already has a slot that overlaps the requested time window.");

            slot.AgentId = request.AgentId;
            slot.StartAtUtc = request.StartAtUtc;
            slot.EndAtUtc = request.EndAtUtc;
            slot.Capacity = request.Capacity;
            slot.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            slot.UpdatedAtUtc = DateTimeOffset.UtcNow;

            _slotRepository.Update(slot);
            await _uow.SaveChanges();

            return Result<InspectionSlotDto>.Success(MapToDto(slot));
        }

        public async Task<Result<InspectionSlotDto>> Cancel(Guid id, CancellationToken ct)
        {
            var slot = await _slotRepository.GetByIdForUpdateAsync(id, ct);
            if (slot is null)
                return Result<InspectionSlotDto>.NotFound("SlotNotFound", "Inspection slot not found.");

            if (slot.Status == InspectionSlotStatus.Cancelled)
                return Result<InspectionSlotDto>.Conflict("SlotAlreadyCancelled", "Slot is already cancelled.");

            var hasActiveBookings = await _bookingRepository.HasActiveBookingsAsync(id, ct);
            if (hasActiveBookings)
                return Result<InspectionSlotDto>.Conflict("ActiveBookingsExist", "Cannot cancel a slot with active bookings.");

            slot.Status = InspectionSlotStatus.Cancelled;
            slot.UpdatedAtUtc = DateTimeOffset.UtcNow;

            _slotRepository.Update(slot);
            await _uow.SaveChanges();

            return Result<InspectionSlotDto>.Success(MapToDto(slot));
        }

        public async Task<Result<IReadOnlyList<InspectionSlotDto>>> GetAvailableSlotsAsync(Guid listingId, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var request = new GetAvailableInspectionSlotsRequest
            {
                ListingId = listingId,
                FromUtc = now,
                ToUtc = now.AddDays(InspectionSlotConstants.AvailabilityWindowDays),
                Limit = InspectionSlotConstants.AvailableSlotsMaxResults
            };

            var slots = await _slotRepository.GetAvailableSlotsAsync(request, ct);

            var dtos = slots.Select(s => new InspectionSlotDto
            {
                Id = s.InspectionSlotId,
                ListingId = s.ListingId,
                PropertyId = s.PropertyId,
                AgentId = s.AgentId,
                StartAtUtc = s.StartAtUtc,
                EndAtUtc = s.EndAtUtc,
                Capacity = s.Capacity,
                Status = s.Status.ToString(),
                Notes = s.Notes
            }).ToList();

            return Result<IReadOnlyList<InspectionSlotDto>>.Success(dtos);
        }

        public async Task<Result<InspectionSlotDto>> GetInspectionSlotByIdAsync(Guid id, CancellationToken ct)
        {
            var slot = await _slotRepository.GetByIdAsync(id, ct);
            if (slot is null)
                return Result<InspectionSlotDto>.NotFound("SlotNotFound", "Inspection slot not found.");

            return Result<InspectionSlotDto>.Success(MapToDto(slot));
        }

        private static InspectionSlotDto MapToDto(InspectionSlot slot) => new()
        {
            Id = slot.Id,
            PropertyId = slot.PropertyId,
            ListingId = slot.ListingId,
            AgentId = slot.AgentId,
            UserId = slot.UserId,
            StartAtUtc = slot.StartAtUtc,
            EndAtUtc = slot.EndAtUtc,
            Capacity = slot.Capacity,
            Status = slot.Status.ToString(),
            Notes = slot.Notes,
            CreatedAtUtc = slot.CreatedAtUtc,
            UpdatedAtUtc = slot.UpdatedAtUtc
        };
    }
}
