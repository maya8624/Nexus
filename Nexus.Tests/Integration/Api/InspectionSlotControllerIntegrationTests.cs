using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;
using Nexus.Tests.Integration.Helpers;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Nexus.Tests.Integration.Api;

// TODO: All endpoints are [AllowAnonymous] for now — add 401 tests for each endpoint once auth is enforced.
public class InspectionSlotControllerIntegrationTests : IntegrationTestBase
{
    #region POST /api/inspection-slots

    [Fact]
    public async Task Create_WithValidRequest_Returns201AndSlot()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var request = new InspectionSlotRequest
        {
            PropertyId = seed.PropertyId,
            ListingId = seed.ListingId,
            AgentId = seed.AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(3).AddHours(1),
            Capacity = 5
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-slots", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<InspectionSlotDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.PropertyId.Should().Be(seed.PropertyId);
        body.AgentId.Should().Be(seed.AgentId);
        body.Capacity.Should().Be(5);
        body.Status.Should().Be("Open");
    }

    [Fact]
    public async Task Create_WithNotes_Returns201AndSlotWithNotes()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var request = new InspectionSlotRequest
        {
            PropertyId = seed.PropertyId,
            AgentId = seed.AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(3).AddHours(1),
            Capacity = 5,
            Notes = "Bring ID to the inspection."
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-slots", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<InspectionSlotDto>(JsonOptions);
        body!.Notes.Should().Be("Bring ID to the inspection.");
    }

    [Fact]
    public async Task Create_WithNonExistentAgent_Returns404()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var request = new InspectionSlotRequest
        {
            PropertyId = seed.PropertyId,
            AgentId = Guid.NewGuid(),
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(3).AddHours(1),
            Capacity = 5
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-slots", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("AgentNotFound");
    }

    [Fact]
    public async Task Create_WithNonExistentProperty_Returns404()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var request = new InspectionSlotRequest
        {
            PropertyId = Guid.NewGuid(),
            AgentId = seed.AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(3).AddHours(1),
            Capacity = 5
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-slots", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("PropertyNotFound");
    }

    [Fact]
    public async Task Create_WithOverlappingSlot_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // Seeded slot is at now+1day to now+1day+1hour — create an overlapping request
        var request = new InspectionSlotRequest
        {
            PropertyId = seed.PropertyId,
            AgentId = seed.AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Capacity = 5
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-slots", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("SlotOverlap");
    }

    #endregion

    #region GET /api/inspection-slots/available

    [Fact]
    public async Task GetAvailable_WithOpenSlots_Returns200AndSlots()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // Act
        var response = await Client.GetAsync($"/api/inspection-slots/available?listingId={seed.ListingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<InspectionSlotDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Should().HaveCount(1);
        body[0].ListingId.Should().Be(seed.ListingId);
        body[0].Status.Should().Be("Open");
    }

    [Fact]
    public async Task GetAvailable_WithNoSlots_Returns200AndEmptyList()
    {
        // Act
        var response = await Client.GetAsync($"/api/inspection-slots/available?listingId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<InspectionSlotDto>>(JsonOptions);
        body.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAvailable_ExcludesCancelledSlots_Returns200AndEmptyList()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var slot = await db.InspectionSlots.FindAsync(seed.SlotId);
        slot!.Status = InspectionSlotStatus.Cancelled;
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/inspection-slots/available?listingId={seed.ListingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<InspectionSlotDto>>(JsonOptions);
        body.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region GET /api/inspection-slots/{id}

    [Fact]
    public async Task GetById_WithExistingSlot_Returns200AndSlot()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // Act
        var response = await Client.GetAsync($"/api/inspection-slots/{seed.SlotId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InspectionSlotDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().Be(seed.SlotId);
        body.PropertyId.Should().Be(seed.PropertyId);
        body.AgentId.Should().Be(seed.AgentId);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        // Act
        var response = await Client.GetAsync($"/api/inspection-slots/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PATCH /api/inspection-slots/{id}

    [Fact]
    public async Task Update_WithValidRequest_Returns200AndUpdatedSlot()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var newStart = DateTimeOffset.UtcNow.AddDays(5);
        var request = new UpdateInspectionSlotRequest
        {
            AgentId = seed.AgentId,
            StartAtUtc = newStart,
            EndAtUtc = newStart.AddHours(2),
            Capacity = 10,
            Notes = "Updated notes."
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/inspection-slots/{seed.SlotId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InspectionSlotDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.Capacity.Should().Be(10);
        body.Notes.Should().Be("Updated notes.");
        body.StartAtUtc.Should().BeCloseTo(newStart, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Update_WithNonExistentSlot_Returns404()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var request = new UpdateInspectionSlotRequest
        {
            AgentId = seed.AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(5).AddHours(1),
            Capacity = 5
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/inspection-slots/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("SlotNotFound");
    }

    [Fact]
    public async Task Update_WithCancelledSlot_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var slot = await db.InspectionSlots.FindAsync(seed.SlotId);
        slot!.Status = InspectionSlotStatus.Cancelled;
        await db.SaveChangesAsync();

        var request = new UpdateInspectionSlotRequest
        {
            AgentId = seed.AgentId,
            StartAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            EndAtUtc = DateTimeOffset.UtcNow.AddDays(5).AddHours(1),
            Capacity = 5
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/inspection-slots/{seed.SlotId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("SlotCancelled");
    }

    #endregion

    #region PATCH /api/inspection-slots/{id}/cancel

    [Fact]
    public async Task Cancel_WithOpenSlot_Returns200AndCancelledStatus()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // Act
        var response = await Client.PatchAsync($"/api/inspection-slots/{seed.SlotId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InspectionSlotDto>(JsonOptions);
        body!.Status.Should().Be("Cancelled");
        body.Id.Should().Be(seed.SlotId);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledSlot_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        var slot = await db.InspectionSlots.FindAsync(seed.SlotId);
        slot!.Status = InspectionSlotStatus.Cancelled;
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PatchAsync($"/api/inspection-slots/{seed.SlotId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("SlotAlreadyCancelled");
    }

    [Fact]
    public async Task Cancel_WithActiveBookings_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        await SeedDataBuilder.AddBookingAsync(db, seed, status: InspectionBookingStatus.Pending);

        // Act
        var response = await Client.PatchAsync($"/api/inspection-slots/{seed.SlotId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("ActiveBookingsExist");
    }

    [Fact]
    public async Task Cancel_WithNonExistentSlot_Returns404()
    {
        // Act
        var response = await Client.PatchAsync($"/api/inspection-slots/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
