namespace Nexus.Domain.Enums
{
    public enum PropertyType
    {
        House = 1,
        Apartment = 2,
        Townhouse = 3,
        Villa = 4,
        Land = 5
    }

    public enum ListingType
    {
        Sale = 1,
        Rent = 2
    }

    public enum ListingStatus
    {
        Draft = 1,
        Active = 2,
        UnderOffer = 3,
        Sold = 4,
        Leased = 5,
        Withdrawn = 6
    }

    public enum EnquiryStatus
    {
        New = 1,
        Read = 2,
        Responded = 3,
        Closed = 4
    }

    public enum InspectionBookingStatus
    {
        Pending = 1,
        Confirmed = 2,
        Cancelled = 3,
        Completed = 4,
        NoShow = 5
    }

    public enum ChatMessageRole
    {
        User = 1,
        Assistant = 2,
        Tool = 3,
        System = 4
    }
}