using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;

namespace Nexus.Application.Interfaces
{
    public interface IBlobStorageService
    {
        Task<Result<SasUploadResponse>> GenerateSasUploadUrlAsync(string fileName, string contentType, string containerName, Guid userId, CancellationToken ct);
    }
}
