namespace Nexus.Domain.Enums
{
    public enum UploadPurpose
    {
        General = 1,
        Extraction = 2,
        Ingestion = 3,
        Invoice = 4
    }

    public enum UploadStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Expired = 4
    }

    public enum IngestionStatus
    {
        Queued = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4
    }
}
