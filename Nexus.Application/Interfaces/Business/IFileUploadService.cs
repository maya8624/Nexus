using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;
using Nexus.Domain.Enums;

namespace Nexus.Application.Interfaces.Business
{
    public interface IFileUploadService
    {
        Task<Result<FileUploadInitiatedResponse>> InitiateAsync(string fileName, string contentType, UploadPurpose purpose, Guid userId, CancellationToken ct);
        Task<Result<FileUploadInitiatedResponse>> ConfirmAsync(Guid id, Guid userId, long? fileSizeBytes, CancellationToken ct);
    }
}
