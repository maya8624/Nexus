namespace Nexus.Application.Interfaces.Business
{
    public interface IInvoiceExtractionJob
    {
        Task ExecuteAsync(Guid fileUploadId);
    }
}
