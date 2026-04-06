using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Persistence;

namespace Nexus.Tests.Integration.Helpers;

public sealed record TestSeedData
{
    public required Guid UserId { get; init; }
    public required string UserEmail { get; init; }
    public required Guid AgentId { get; init; }
    public required Guid PropertyId { get; init; }
    public required Guid ListingId { get; init; }
    public required Guid SlotId { get; init; }
}

public static class SeedDataBuilder
{
    public static async Task<TestSeedData> SeedAsync(AppDbContext db, int propertyTypeId)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var userEmail = $"user-{userId:N}@test.com";

        db.Agents.Add(new Agent
        {
            Id = agentId,
            FirstName = "Test",
            LastName = "Agent",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.Properties.Add(new Property
        {
            Id = propertyId,
            PropertyTypeId = propertyTypeId,
            Title = "Test Property",
            Bedrooms = 3,
            Bathrooms = 2,
            CarSpaces = 1,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.Listings.Add(new Listing
        {
            Id = listingId,
            PropertyId = propertyId,
            ListingType = ListingType.Sale,
            Status = ListingStatus.Active,
            Price = 500_000m,
            ListedAtUtc = now,
            IsPublished = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.Users.Add(new User
        {
            Id = userId,
            Email = userEmail,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.InspectionSlots.Add(new InspectionSlot
        {
            Id = slotId,
            UserId = userId,
            PropertyId = propertyId,
            ListingId = listingId,
            AgentId = agentId,
            StartAtUtc = now.AddDays(1),
            EndAtUtc = now.AddDays(1).AddHours(1),
            Capacity = 5,
            Status = InspectionSlotStatus.Open,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await db.SaveChangesAsync();

        return new TestSeedData
        {
            UserId = userId,
            UserEmail = userEmail,
            AgentId = agentId,
            PropertyId = propertyId,
            ListingId = listingId,
            SlotId = slotId
        };
    }

    public static async Task<InspectionBooking> AddBookingAsync(
        AppDbContext db,
        TestSeedData seed,
        Guid? userId = null,
        InspectionBookingStatus status = InspectionBookingStatus.Pending)
    {
        var now = DateTimeOffset.UtcNow;
        var booking = new InspectionBooking
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? seed.UserId,
            InspectionSlotId = seed.SlotId,
            PropertyId = seed.PropertyId,
            ListingId = seed.ListingId,
            AgentId = seed.AgentId,
            Status = status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.InspectionBookings.Add(booking);
        await db.SaveChangesAsync();

        return booking;
    }

    public static async Task<User> AddUserAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"user-{userId:N}@test.com",
            FirstName = "Extra",
            LastName = "User",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }
}
