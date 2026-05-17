namespace Nexus.Domain.Entities
{
    public class TenantPreferences
    {
        public Guid UserId { get; set; }
        public string[] Suburbs { get; set; } = [];
        public int MaxRent { get; set; }
        public int MinBeds { get; set; }
        public int MaxBeds { get; set; }
        public bool PetFriendly { get; set; }
        public int AvailableWithinDays { get; set; }

        public User User { get; set; } = null!;
    }
}
