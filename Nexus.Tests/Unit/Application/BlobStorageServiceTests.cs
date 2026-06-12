using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Moq;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Services;
using Nexus.Application.Settings;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class BlobStorageServiceTests
    {
        private const string ContainerName = "test-container";
        private const int SasExpiryMinutes = 15;

        private readonly Mock<BlobServiceClient> _blobServiceClientMock = new();
        private readonly Mock<BlobContainerClient> _containerClientMock = new();
        private readonly Mock<BlobClient> _blobClientMock = new();
        private readonly BlobStorageService _service;

        public BlobStorageServiceTests()
        {
            var settings = Options.Create(new BlobStorageSettings
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = ContainerName,
                ExtractionContainerName = "test-extraction-container",
                IngestionContainerName = "test-ingestion-container",
                SasExpiryMinutes = SasExpiryMinutes
            });

            _blobServiceClientMock
                .Setup(x => x.GetBlobContainerClient(ContainerName))
                .Returns(_containerClientMock.Object);

            _blobClientMock
                .Setup(x => x.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(new Uri("https://test.blob.core.windows.net/test-container/blob.pdf?sig=abc"));

            _service = new BlobStorageService(settings, _blobServiceClientMock.Object);
        }

        [Fact]
        public async Task GenerateSasUploadUrl_ReturnsSuccess()
        {
            var userId = Guid.NewGuid();
            _containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            var result = await _service.GenerateSasUploadUrlAsync("document.pdf", "application/pdf", ContainerName, userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GenerateSasUploadUrl_BlobName_IsUserScopedWithExtension()
        {
            var userId = Guid.NewGuid();
            string? capturedBlobName = null;

            _containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Callback<string>(name => capturedBlobName = name)
                .Returns(_blobClientMock.Object);

            await _service.GenerateSasUploadUrlAsync("document.pdf", "application/pdf", ContainerName, userId, CancellationToken.None);

            Assert.NotNull(capturedBlobName);
            Assert.StartsWith($"{userId}/", capturedBlobName);
            Assert.EndsWith(".pdf", capturedBlobName);
        }

        [Fact]
        public async Task GenerateSasUploadUrl_BlobNameInResponse_MatchesBlobClientName()
        {
            var userId = Guid.NewGuid();
            string? capturedBlobName = null;

            _containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Callback<string>(name => capturedBlobName = name)
                .Returns(_blobClientMock.Object);

            var result = await _service.GenerateSasUploadUrlAsync("photo.jpg", "image/jpeg", ContainerName, userId, CancellationToken.None);

            Assert.Equal(capturedBlobName, result.Value!.BlobName);
        }

        [Fact]
        public async Task GenerateSasUploadUrl_SasUrl_IsReturnedFromClient()
        {
            var userId = Guid.NewGuid();
            var expectedUri = new Uri("https://test.blob.core.windows.net/test-container/file.png?sig=xyz");

            _blobClientMock
                .Setup(x => x.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedUri);

            _containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            var result = await _service.GenerateSasUploadUrlAsync("photo.png", "image/png", ContainerName, userId, CancellationToken.None);

            Assert.Equal(expectedUri.ToString(), result.Value!.SasUrl);
        }

        [Fact]
        public async Task GenerateSasUploadUrl_SasBuilder_HasCorrectPermissionsAndContainer()
        {
            var userId = Guid.NewGuid();
            BlobSasBuilder? capturedBuilder = null;

            _blobClientMock
                .Setup(x => x.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Callback<BlobSasBuilder>(b => capturedBuilder = b)
                .Returns(new Uri("https://test.blob.core.windows.net/blob?sig=x"));

            _containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            await _service.GenerateSasUploadUrlAsync("document.pdf", "application/pdf", ContainerName, userId, CancellationToken.None);

            Assert.NotNull(capturedBuilder);
            Assert.Equal(ContainerName, capturedBuilder!.BlobContainerName);
            Assert.Equal("b", capturedBuilder.Resource);
            Assert.Contains("w", capturedBuilder.Permissions);
            Assert.Contains("c", capturedBuilder.Permissions);
        }

        [Fact]
        public async Task GenerateSasUploadUrl_TwoCalls_ProduceDifferentBlobNames()
        {
            var userId = Guid.NewGuid();
            var capturedNames = new List<string>();

            _containerClientMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Callback<string>(name => capturedNames.Add(name))
                .Returns(_blobClientMock.Object);

            await _service.GenerateSasUploadUrlAsync("a.pdf", "application/pdf", ContainerName, userId, CancellationToken.None);
            await _service.GenerateSasUploadUrlAsync("b.pdf", "application/pdf", ContainerName, userId, CancellationToken.None);

            Assert.NotEqual(capturedNames[0], capturedNames[1]);
        }
    }

    [Trait("Category", "Unit")]
    public class GetUploadUrlRequestValidatorTests
    {
        private readonly GetUploadUrlRequestValidator _validator = new();

        [Fact]
        public void Validate_ValidRequest_Passes()
        {
            var result = _validator.Validate(new GetUploadUrlRequest
            {
                FileName = "document.pdf",
                ContentType = "application/pdf"
            });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_EmptyFileName_Fails()
        {
            var result = _validator.Validate(new GetUploadUrlRequest
            {
                FileName = "",
                ContentType = "application/pdf"
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUploadUrlRequest.FileName));
        }

        [Fact]
        public void Validate_FileNameTooLong_Fails()
        {
            var result = _validator.Validate(new GetUploadUrlRequest
            {
                FileName = new string('a', 256),
                ContentType = "application/pdf"
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUploadUrlRequest.FileName));
        }

        [Fact]
        public void Validate_UnsupportedContentType_Fails()
        {
            var result = _validator.Validate(new GetUploadUrlRequest
            {
                FileName = "script.exe",
                ContentType = "application/x-msdownload"
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUploadUrlRequest.ContentType));
        }

        [Fact]
        public void Validate_EmptyContentType_Fails()
        {
            var result = _validator.Validate(new GetUploadUrlRequest
            {
                FileName = "document.pdf",
                ContentType = ""
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUploadUrlRequest.ContentType));
        }

        [Theory]
        [InlineData("image/jpeg")]
        [InlineData("image/png")]
        [InlineData("image/webp")]
        [InlineData("application/pdf")]
        [InlineData("application/msword")]
        [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        public void Validate_AllAllowedContentTypes_Pass(string contentType)
        {
            var result = _validator.Validate(new GetUploadUrlRequest
            {
                FileName = "file.bin",
                ContentType = contentType
            });

            Assert.True(result.IsValid);
        }
    }
}
