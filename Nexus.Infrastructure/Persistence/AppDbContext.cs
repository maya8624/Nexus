using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Persistence.Configurations;

namespace Nexus.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        //TODO: Remove later
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        ///
        
        public DbSet<Agency> Agencies => Set<Agency>();
        public DbSet<Agent> Agencts => Set<Agent>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<Enquiry> Enquiries => Set<Enquiry>();
        public DbSet<InspectionBooking> InspectionBookings => Set<InspectionBooking>();
        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<PropertyAddress> PropertyAddresses => Set<PropertyAddress>();
        public DbSet<Property> Properties => Set<Property>();
        public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
        public DbSet<PropertyType> PropertyTypes => Set<PropertyType>();
        public DbSet<SavedProperty> SavedProperties => Set<SavedProperty>();
        public DbSet<ToolExecution> ToolExecutions => Set<ToolExecution>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserLogin> UserLogins => Set<UserLogin>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
