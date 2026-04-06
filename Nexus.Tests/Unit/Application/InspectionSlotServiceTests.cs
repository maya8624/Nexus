using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.ReadModels;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using System.Linq.Expressions;
using Xunit;

namespace Nexus.Tests.Application
{
    public class InspectionSlotServiceTests
    {
        private readonly Mock<IInspectionSlotRepository> _slotRepository = new();
        private readonly Mock<IInspectionBookingRepository> _bookingRepository = new();
        private readonly Mock<IAgentRepository> _agentRepository = new();
        private readonly Mock<IPropertyRepository> _propertyRepository = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly IInspectionSlotService _service;

        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid AgentId = Guid.NewGuid();
        private static readonly Guid PropertyId = Guid.NewGuid();
        private static readonly Guid ListingId = Guid.NewGuid();
        private static readonly Guid SlotId = Guid.NewGuid();
        private static readonly CancellationToken Ct = CancellationToken.None;

        public InspectionSlotServiceTests()
        {
            _userContext.Setup(x => x.UserId).Returns(UserId.ToString());

            _service = new InspectionSlotService(
                _slotRepository.Object,
                _bookingRepository.Object,
                _agentRepository.Object,
                _propertyRepository.Object,
                _userContext.Object,
                _uow.Object);
        }

        #region CreateAsync

        [Fact]
        public async Task CreateSlot_WithMissingAgent_ShouldReturnNotFound()
        {
            _agentRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Agent, bool>>>(), Ct))
                .ReturnsAsync(false);

            var result = await _service.CreateAsync(BuildCreateRequest(), Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("AgentNotFound", Assert.Single(result.Errors).Code);
            _slotRepository.Verify(x => x.Create(It.IsAny<InspectionSlot>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CreateSlot_WithMissingProperty_ShouldReturnNotFound()
        {
            _agentRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Agent, bool>>>(), Ct))
                .ReturnsAsync(true);

            _propertyRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Property, bool>>>(), Ct))
                .ReturnsAsync(false);

            var result = await _service.CreateAsync(BuildCreateRequest(), Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("PropertyNotFound", Assert.Single(result.Errors).Code);
            _slotRepository.Verify(x => x.HasConflictingSlotAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
            _slotRepository.Verify(x => x.Create(It.IsAny<InspectionSlot>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CreateSlot_WithConflictingSlot_ShouldReturnConflict()
        {
            var request = BuildCreateRequest();

            _agentRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Agent, bool>>>(), Ct))
                .ReturnsAsync(true);

            _propertyRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Property, bool>>>(), Ct))
                .ReturnsAsync(true);

            _slotRepository
                .Setup(x => x.HasConflictingSlotAsync(request.PropertyId, request.AgentId, request.StartAtUtc, request.EndAtUtc, Ct, null))
                .ReturnsAsync(true);

            var result = await _service.CreateAsync(request, Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("SlotOverlap", Assert.Single(result.Errors).Code);
            _slotRepository.Verify(x => x.Create(It.IsAny<InspectionSlot>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CreateSlot_WithValidInput_ShouldReturnCreatedSlot()
        {
            var request = BuildCreateRequest();
            InspectionSlot? createdSlot = null;

            _agentRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Agent, bool>>>(), Ct))
                .ReturnsAsync(true);

            _propertyRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Property, bool>>>(), Ct))
                .ReturnsAsync(true);

            _slotRepository
                .Setup(x => x.HasConflictingSlotAsync(request.PropertyId, request.AgentId, request.StartAtUtc, request.EndAtUtc, Ct, null))
                .ReturnsAsync(false);

            _slotRepository
                .Setup(x => x.Create(It.IsAny<InspectionSlot>(), Ct))
                .Callback<InspectionSlot, CancellationToken>((slot, _) => createdSlot = slot)
                .Returns(Task.CompletedTask);

            _uow.Setup(x => x.SaveChanges()).ReturnsAsync(1);

            var result = await _service.CreateAsync(request, Ct);

            Assert.True(result.IsSuccess);
            Assert.NotNull(createdSlot);
            Assert.NotNull(result.Value);
            Assert.Equal(createdSlot!.Id, result.Value!.Id);
            Assert.Equal(request.PropertyId, createdSlot.PropertyId);
            Assert.Equal(request.ListingId, createdSlot.ListingId);
            Assert.Equal(request.AgentId, createdSlot.AgentId);
            Assert.Equal(UserId, createdSlot.UserId);
            Assert.Equal(request.StartAtUtc, createdSlot.StartAtUtc);
            Assert.Equal(request.EndAtUtc, createdSlot.EndAtUtc);
            Assert.Equal(request.Capacity, createdSlot.Capacity);
            Assert.Equal(InspectionSlotStatus.Open, createdSlot.Status);
            Assert.Equal("Front door access", createdSlot.Notes);
            Assert.True(createdSlot.CreatedAtUtc != default);
            Assert.Equal(createdSlot.CreatedAtUtc, createdSlot.UpdatedAtUtc);
            Assert.Equal(createdSlot.Id, result.Value.Id);
            Assert.Equal(createdSlot.PropertyId, result.Value.PropertyId);
            Assert.Equal(createdSlot.ListingId, result.Value.ListingId);
            Assert.Equal(createdSlot.AgentId, result.Value.AgentId);
            Assert.Equal(createdSlot.UserId, result.Value.UserId);
            Assert.Equal(createdSlot.StartAtUtc, result.Value.StartAtUtc);
            Assert.Equal(createdSlot.EndAtUtc, result.Value.EndAtUtc);
            Assert.Equal(createdSlot.Capacity, result.Value.Capacity);
            Assert.Equal(createdSlot.Status.ToString(), result.Value.Status);
            Assert.Equal(createdSlot.Notes, result.Value.Notes);
            _slotRepository.Verify(x => x.HasConflictingSlotAsync(request.PropertyId, request.AgentId, request.StartAtUtc, request.EndAtUtc, Ct, null), Times.Once);
            _slotRepository.Verify(x => x.Create(It.IsAny<InspectionSlot>(), Ct), Times.Once);
            _uow.Verify(x => x.SaveChanges(), Times.Once);
        }

        #endregion

        #region Update

        [Fact]
        public async Task UpdateSlot_WithMissingSlot_ShouldReturnNotFound()
        {
            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync((InspectionSlot?)null);

            var result = await _service.Update(SlotId, BuildUpdateRequest(), Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task UpdateSlot_WithCancelledSlot_ShouldReturnConflict()
        {
            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync(BuildSlot(InspectionSlotStatus.Cancelled));

            var result = await _service.Update(SlotId, BuildUpdateRequest(), Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task UpdateSlot_WithMissingAgent_ShouldReturnNotFound()
        {
            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync(BuildSlot(InspectionSlotStatus.Open));

            _agentRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Agent, bool>>>(), Ct))
                .ReturnsAsync(false);

            var result = await _service.Update(SlotId, BuildUpdateRequest(), Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            _slotRepository.Verify(x => x.HasConflictingSlotAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task UpdateSlot_WithConflictingSlot_ShouldReturnConflict()
        {
            var request = BuildUpdateRequest();

            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync(BuildSlot(InspectionSlotStatus.Open));

            _agentRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Agent, bool>>>(), Ct))
                .ReturnsAsync(true);

            _slotRepository
                .Setup(x => x.HasConflictingSlotAsync(PropertyId, request.AgentId, request.StartAtUtc, request.EndAtUtc, Ct, SlotId))
                .ReturnsAsync(true);

            var result = await _service.Update(SlotId, request, Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            _slotRepository.Verify(x => x.Update(It.IsAny<InspectionSlot>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task UpdateSlot_WithValidInput_ShouldReturnUpdatedDto()
        {
            var slot = BuildSlot(InspectionSlotStatus.Open);
            var request = BuildUpdateRequest();
            var previousUpdatedAtUtc = slot.UpdatedAtUtc;

            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync(slot);

            _agentRepository
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<Agent, bool>>>(), Ct))
                .ReturnsAsync(true);

            _slotRepository
                .Setup(x => x.HasConflictingSlotAsync(PropertyId, request.AgentId, request.StartAtUtc, request.EndAtUtc, Ct, SlotId))
                .ReturnsAsync(false);

            _uow.Setup(x => x.SaveChanges()).ReturnsAsync(1);

            var result = await _service.Update(SlotId, request, Ct);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(request.AgentId, result.Value!.AgentId);
            Assert.Equal(request.StartAtUtc, result.Value.StartAtUtc);
            Assert.Equal(request.EndAtUtc, result.Value.EndAtUtc);
            Assert.Equal(request.Capacity, result.Value.Capacity);
            Assert.Equal("Bring brochure", result.Value.Notes);
            Assert.Equal(request.AgentId, slot.AgentId);
            Assert.Equal(request.StartAtUtc, slot.StartAtUtc);
            Assert.Equal(request.EndAtUtc, slot.EndAtUtc);
            Assert.Equal(request.Capacity, slot.Capacity);
            Assert.Equal("Bring brochure", slot.Notes);
            Assert.True(slot.UpdatedAtUtc >= previousUpdatedAtUtc);
            _slotRepository.Verify(x => x.HasConflictingSlotAsync(PropertyId, request.AgentId, request.StartAtUtc, request.EndAtUtc, Ct, SlotId), Times.Once);
            _slotRepository.Verify(x => x.Update(slot), Times.Once);
            _uow.Verify(x => x.SaveChanges(), Times.Once);
        }

        #endregion

        #region Cancel

        [Fact]
        public async Task CancelSlot_WithMissingSlot_ShouldReturnNotFound()
        {
            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync((InspectionSlot?)null);

            var result = await _service.Cancel(SlotId, Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            _bookingRepository.Verify(x => x.HasActiveBookingsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CancelSlot_WithAlreadyCancelledSlot_ShouldReturnConflict()
        {
            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync(BuildSlot(InspectionSlotStatus.Cancelled));

            var result = await _service.Cancel(SlotId, Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            _bookingRepository.Verify(x => x.HasActiveBookingsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CancelSlot_WithActiveBookings_ShouldReturnConflict()
        {
            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync(BuildSlot(InspectionSlotStatus.Open));

            _bookingRepository
                .Setup(x => x.HasActiveBookingsAsync(SlotId, Ct))
                .ReturnsAsync(true);

            var result = await _service.Cancel(SlotId, Ct);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            _slotRepository.Verify(x => x.Update(It.IsAny<InspectionSlot>()), Times.Never);
            _uow.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task CancelSlot_WithValidInput_ShouldReturnCancelledSlot()
        {
            var slot = BuildSlot(InspectionSlotStatus.Open);
            var previousUpdatedAtUtc = slot.UpdatedAtUtc;

            _slotRepository
                .Setup(x => x.GetByIdForUpdateAsync(SlotId, Ct))
                .ReturnsAsync(slot);

            _bookingRepository
                .Setup(x => x.HasActiveBookingsAsync(SlotId, Ct))
                .ReturnsAsync(false);

            _uow.Setup(x => x.SaveChanges()).ReturnsAsync(1);

            var result = await _service.Cancel(SlotId, Ct);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(InspectionSlotStatus.Cancelled.ToString(), result.Value!.Status);
            Assert.Equal(InspectionSlotStatus.Cancelled, slot.Status);
            Assert.True(slot.UpdatedAtUtc >= previousUpdatedAtUtc);
            _slotRepository.Verify(x => x.Update(slot), Times.Once);
            _uow.Verify(x => x.SaveChanges(), Times.Once);
        }

        #endregion

        #region GetAvailableSlotsAsync

        [Fact]
        public async Task GetAvailableSlots_WithNoOpenSlots_ShouldReturnEmptyList()
        {
            _slotRepository
                .Setup(x => x.GetAvailableSlotsAsync(It.IsAny<GetAvailableInspectionSlotsRequest>(), Ct))
                .ReturnsAsync(new List<AvailableInspectionSlotReadModel>());

            var result = await _service.GetAvailableSlotsAsync(ListingId, Ct);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public async Task GetAvailableSlots_WithMatchingSlots_ShouldReturnMappedDtos()
        {
            var readModels = new List<AvailableInspectionSlotReadModel>
            {
                new()
                {
                    InspectionSlotId = SlotId,
                    ListingId = ListingId,
                    PropertyId = PropertyId,
                    AgentId = AgentId,
                    StartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                    EndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                    Capacity = 5,
                    Status = InspectionSlotStatus.Open,
                    ActiveBookingCount = 2,
                    RemainingCapacity = 3
                }
            };

            _slotRepository
                .Setup(x => x.GetAvailableSlotsAsync(It.IsAny<GetAvailableInspectionSlotsRequest>(), Ct))
                .ReturnsAsync(readModels);

            var result = await _service.GetAvailableSlotsAsync(ListingId, Ct);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            var dto = result.Value!.First();
            Assert.Equal(SlotId, dto.Id);
            Assert.Equal(ListingId, dto.ListingId);
            Assert.Equal(InspectionSlotStatus.Open.ToString(), dto.Status);
        }

        #endregion

        #region Helpers

        private static CreateInspectionSlotRequest BuildCreateRequest() => new()
        {
            PropertyId = PropertyId,
            ListingId = ListingId,
            AgentId = AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Capacity = 5,
            Notes = "  Front door access  "
        };

        private static UpdateInspectionSlotRequest BuildUpdateRequest() => new()
        {
            AgentId = AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(2),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(2).AddHours(1),
            Capacity = 3,
            Notes = "  Bring brochure  "
        };

        private static InspectionSlot BuildSlot(InspectionSlotStatus status) => new()
        {
            Id = SlotId,
            PropertyId = PropertyId,
            ListingId = ListingId,
            AgentId = AgentId,
            UserId = UserId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            Capacity = 5,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            RowVersion = []
        };

        #endregion
    }
}
