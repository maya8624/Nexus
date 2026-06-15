namespace Nexus.Application.Interfaces.Business
{
    public interface IIngestionJob
    {
        Task ExecuteAsync(Guid fileUploadId);
    }
}
