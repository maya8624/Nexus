using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;
using Nexus.Application.Settings;

namespace Nexus.Application.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobStorageSettings _settings;
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(IOptions<BlobStorageSettings> settings, BlobServiceClient blobServiceClient)
        {
            _settings = settings.Value;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<byte[]> DownloadBlobAsync(string containerName, string blobName, CancellationToken ct)
        {
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync(ct);
            return response.Value.Content.ToArray();
        }

        public Task<Result<SasUploadResponse>> GenerateSasUploadUrlAsync(string fileName, string contentType, string containerName, Guid userId, CancellationToken ct)
        {
            var extension = Path.GetExtension(fileName);
            var blobName = $"{userId}/{Guid.NewGuid()}{extension}";

            var blobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_settings.SasExpiryMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

            return Task.FromResult(Result<SasUploadResponse>.Success(new SasUploadResponse
            {
                SasUrl = sasUrl,
                BlobName = blobName
            }));
        }
    }
}
