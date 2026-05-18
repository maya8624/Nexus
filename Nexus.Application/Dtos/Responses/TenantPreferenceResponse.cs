namespace Nexus.Application.Dtos.Responses
{
    public class TenantPreferenceResponse
    {
        public string[] Suburbs { get; set; } = [];
        public int MaxRent { get; set; }
        public int MinBeds { get; set; }
        public int MaxBeds { get; set; }
        public bool PetFriendly { get; set; }
        public int AvailableWithinDays { get; set; }
    }
}
