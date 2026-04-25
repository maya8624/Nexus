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

public class InspectionBookingControllerIntegrationTests : IntegrationTestBase
{
    #region POST /api/inspection-bookings

    [Fact]
    public async Task Create_WithValidRequest_Returns201AndBooking()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);
        var request = new InspectionBookingRequest { InspectionSlotId = seed.SlotId };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<InspectionBookingDto>(JsonOptions);
        body.Should().NotBeNull();
        //body!.InspectionSlotId.Should().Be(seed.SlotId);
        body.UserId.Should().Be(seed.UserId);
        body.PropertyId.Should().Be(seed.PropertyId);
        //body.AgentId.Should().Be(seed.AgentId);
        body.Status.Should().Be("Pending");
    }

    //[Fact]
    //public async Task Create_WithNotes_Returns201AndBookingWithNotes()
    //{
    //    // Arrange
    //    using var scope = CreateScope();
    //    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //    var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

    //    JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);
    //    var request = new InspectionBookingRequest
    //    {
    //        InspectionSlotId = seed.SlotId,
    //        Notes = "Please use the side entrance."
    //    };

    //    // Act
    //    var response = await Client.PostAsJsonAsync("/api/inspection-bookings", request);

    //    // Assert
    //    response.StatusCode.Should().Be(HttpStatusCode.Created);

    //    var body = await response.Content.ReadFromJsonAsync<InspectionBookingDto>(JsonOptions);
    //    body!.Notes.Should().Be("Please use the side entrance.");
    //}

    [Fact]
    public async Task Create_WhenSlotFull_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // Fill the slot: capacity is 5, add 5 confirmed bookings from other users
        for (var i = 0; i < 5; i++)
        {
            var otherUser = await SeedDataBuilder.AddUserAsync(db);
            await SeedDataBuilder.AddBookingAsync(db, seed, userId: otherUser.Id, status: InspectionBookingStatus.Confirmed);
        }

        // New user tries to book the full slot
        var newUser = await SeedDataBuilder.AddUserAsync(db);
        JwtTokenHelper.AuthenticateClient(Client, newUser.Id, newUser.Email);
        var request = new InspectionBookingRequest { InspectionSlotId = seed.SlotId };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("SlotFull");
    }

    [Fact]
    public async Task Create_WhenSlotNotOpen_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // Cancel the slot directly in DB
        var slot = await db.InspectionSlots.FindAsync(seed.SlotId);
        slot!.Status = InspectionSlotStatus.Cancelled;
        await db.SaveChangesAsync();

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);
        var request = new InspectionBookingRequest { InspectionSlotId = seed.SlotId };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("SlotNotAvailable");
    }

    [Fact]
    public async Task Create_WithDuplicateBooking_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // User already has a pending booking for this slot
        await SeedDataBuilder.AddBookingAsync(db, seed, status: InspectionBookingStatus.Pending);

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);
        var request = new InspectionBookingRequest { InspectionSlotId = seed.SlotId };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("DuplicateBooking");
    }

    [Fact]
    public async Task Create_WithNonExistentSlot_Returns404()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedDataBuilder.AddUserAsync(db);

        JwtTokenHelper.AuthenticateClient(Client, user.Id, user.Email);
        var request = new InspectionBookingRequest { InspectionSlotId = Guid.NewGuid() };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WhenUnauthenticated_Returns401()
    {
        // Arrange
        JwtTokenHelper.ClearAuthentication(Client);
        var request = new InspectionBookingRequest { InspectionSlotId = Guid.NewGuid() };

        // Act
        var response = await Client.PostAsJsonAsync("/api/inspection-bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/inspection-bookings/my

    [Fact]
    public async Task GetMyBookings_Returns200AndOnlyOwnBookings()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);

        // Add 2 bookings for this user
        await SeedDataBuilder.AddBookingAsync(db, seed);
        await SeedDataBuilder.AddBookingAsync(db, seed);

        // Add 1 booking for a different user on the same slot
        var otherUser = await SeedDataBuilder.AddUserAsync(db);
        await SeedDataBuilder.AddBookingAsync(db, seed, userId: otherUser.Id);

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);

        // Act
        var response = await Client.GetAsync("/api/inspection-bookings/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<InspectionBookingDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
        body.Should().AllSatisfy(b => b.UserId.Should().Be(seed.UserId));
    }

    [Fact]
    public async Task GetMyBookings_WithNoBookings_Returns200AndEmptyList()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedDataBuilder.AddUserAsync(db);

        JwtTokenHelper.AuthenticateClient(Client, user.Id, user.Email);

        // Act
        var response = await Client.GetAsync("/api/inspection-bookings/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<InspectionBookingDto>>(JsonOptions);
        body.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetMyBookings_WhenUnauthenticated_Returns401()
    {
        // Arrange
        JwtTokenHelper.ClearAuthentication(Client);

        // Act
        var response = await Client.GetAsync("/api/inspection-bookings/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/inspection-bookings/{id}

    [Fact]
    public async Task GetById_WithOwnBooking_Returns200AndBooking()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);
        var booking = await SeedDataBuilder.AddBookingAsync(db, seed);

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);

        // Act
        var response = await Client.GetAsync($"/api/inspection-bookings/{booking.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InspectionBookingDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().Be(booking.Id);
        body.UserId.Should().Be(seed.UserId);
    }

    [Fact]
    public async Task GetById_WithAnotherUsersBooking_Returns404()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);
        var booking = await SeedDataBuilder.AddBookingAsync(db, seed);

        // Authenticate as a different user
        var otherUser = await SeedDataBuilder.AddUserAsync(db);
        JwtTokenHelper.AuthenticateClient(Client, otherUser.Id, otherUser.Email);

        // Act
        var response = await Client.GetAsync($"/api/inspection-bookings/{booking.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedDataBuilder.AddUserAsync(db);

        JwtTokenHelper.AuthenticateClient(Client, user.Id, user.Email);

        // Act
        var response = await Client.GetAsync($"/api/inspection-bookings/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenUnauthenticated_Returns401()
    {
        // Arrange
        JwtTokenHelper.ClearAuthentication(Client);

        // Act
        var response = await Client.GetAsync($"/api/inspection-bookings/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PATCH /api/inspection-bookings/{id}/cancel

    [Fact]
    public async Task Cancel_WithPendingBooking_Returns200AndCancelledStatus()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);
        var booking = await SeedDataBuilder.AddBookingAsync(db, seed, status: InspectionBookingStatus.Pending);

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);

        // Act
        var response = await Client.PatchAsync($"/api/inspection-bookings/{booking.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InspectionBookingDto>(JsonOptions);
        body!.Status.Should().Be("Cancelled");
        body.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task Cancel_WithConfirmedBooking_Returns200AndCancelledStatus()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);
        var booking = await SeedDataBuilder.AddBookingAsync(db, seed, status: InspectionBookingStatus.Confirmed);

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);

        // Act
        var response = await Client.PatchAsync($"/api/inspection-bookings/{booking.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InspectionBookingDto>(JsonOptions);
        body!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledBooking_Returns409()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seed = await SeedDataBuilder.SeedAsync(db, PropertyTypeId);
        var booking = await SeedDataBuilder.AddBookingAsync(db, seed, status: InspectionBookingStatus.Cancelled);

        JwtTokenHelper.AuthenticateClient(Client, seed.UserId, seed.UserEmail);

        // Act
        var response = await Client.PatchAsync($"/api/inspection-bookings/{booking.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        error!.Name.Should().Be("InvalidStatus");
    }

    [Fact]
    public async Task Cancel_WithNonExistentBooking_Returns404()
    {
        // Arrange
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedDataBuilder.AddUserAsync(db);

        JwtTokenHelper.AuthenticateClient(Client, user.Id, user.Email);

        // Act
        var response = await Client.PatchAsync($"/api/inspection-bookings/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_WhenUnauthenticated_Returns401()
    {
        // Arrange
        JwtTokenHelper.ClearAuthentication(Client);

        // Act
        var response = await Client.PatchAsync($"/api/inspection-bookings/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
