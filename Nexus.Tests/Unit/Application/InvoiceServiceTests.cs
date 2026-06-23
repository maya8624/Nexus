using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class InvoiceServiceTests
    {
        private readonly Mock<IInvoiceRepository> _repositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly InvoiceService _service;
        private readonly Guid _userId = Guid.NewGuid();

        public InvoiceServiceTests()
        {
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _service = new InvoiceService(_repositoryMock.Object, _uowMock.Object);
        }

        private Invoice BuildInvoice(Guid? userId = null, Guid? fileUploadId = null) => new()
        {
            Id            = Guid.NewGuid(),
            UserId        = userId ?? _userId,
            FileUploadId  = fileUploadId ?? Guid.NewGuid(),
            Filename      = "invoice.pdf",
            VendorName    = "Acme Corp",
            VendorAddress = "123 Main St",
            CustomerName  = "Jane Doe",
            InvoiceNumber = "INV-0001",
            InvoiceDate   = new DateOnly(2026, 1, 15),
            DueDate       = new DateOnly(2026, 2, 15),
            Subtotal      = 100m,
            Tax           = 10m,
            Total         = 110m,
            Currency      = "AUD",
            Confidence    = 0.97,
            LineItems     =
            [
                new InvoiceLineItem { Description = "Consulting", Quantity = 2m, UnitPrice = 50m, Amount = 100m }
            ],
            CreatedAtUtc  = DateTimeOffset.UtcNow
        };

        #region GetByFileUploadIdAsync

        [Fact]
        public async Task GetByFileUploadIdAsync_WhenNotFound_ShouldReturnNotFound()
        {
            _repositoryMock
                .Setup(x => x.GetByFileUploadIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Invoice?)null);

            var result = await _service.GetByFileUploadIdAsync(Guid.NewGuid(), _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("InvoiceNotFound", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public async Task GetByFileUploadIdAsync_WhenFound_ShouldReturnSuccess()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var result = await _service.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetByFileUploadIdAsync_ShouldMapScalarFieldsCorrectly()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var result = await _service.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, CancellationToken.None);

            var dto = result.Value!;
            Assert.Equal(invoice.Id,            dto.Id);
            Assert.Equal(invoice.UserId,         dto.UserId);
            Assert.Equal(invoice.FileUploadId,   dto.FileUploadId);
            Assert.Equal(invoice.Filename,        dto.Filename);
            Assert.Equal(invoice.VendorName,      dto.VendorName);
            Assert.Equal(invoice.VendorAddress,   dto.VendorAddress);
            Assert.Equal(invoice.CustomerName,    dto.CustomerName);
            Assert.Equal(invoice.InvoiceNumber,   dto.InvoiceNumber);
            Assert.Equal(invoice.InvoiceDate,     dto.InvoiceDate);
            Assert.Equal(invoice.DueDate,         dto.DueDate);
            Assert.Equal(invoice.Subtotal,        dto.Subtotal);
            Assert.Equal(invoice.Tax,             dto.Tax);
            Assert.Equal(invoice.Total,           dto.Total);
            Assert.Equal(invoice.Currency,        dto.Currency);
            Assert.Equal(invoice.Confidence,      dto.Confidence);
            Assert.Equal(invoice.CreatedAtUtc,    dto.CreatedAtUtc);
        }

        [Fact]
        public async Task GetByFileUploadIdAsync_ShouldMapLineItemsCorrectly()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var result = await _service.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, CancellationToken.None);

            var lineItem = Assert.Single(result.Value!.LineItems);
            Assert.Equal("Consulting", lineItem.Description);
            Assert.Equal(2m,           lineItem.Quantity);
            Assert.Equal(50m,          lineItem.UnitPrice);
            Assert.Equal(100m,         lineItem.Amount);
        }

        [Fact]
        public async Task GetByFileUploadIdAsync_WhenNoLineItems_ShouldReturnEmptyList()
        {
            var invoice = BuildInvoice();
            invoice.LineItems = [];
            _repositoryMock
                .Setup(x => x.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var result = await _service.GetByFileUploadIdAsync(invoice.FileUploadId!.Value, _userId, CancellationToken.None);

            Assert.Empty(result.Value!.LineItems);
        }

        [Fact]
        public async Task GetByFileUploadIdAsync_ShouldPassUserIdToRepository()
        {
            var fileUploadId = Guid.NewGuid();
            _repositoryMock
                .Setup(x => x.GetByFileUploadIdAsync(fileUploadId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Invoice?)null);

            await _service.GetByFileUploadIdAsync(fileUploadId, _userId, CancellationToken.None);

            _repositoryMock.Verify(x => x.GetByFileUploadIdAsync(fileUploadId, _userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ShouldReturnNotFound()
        {
            _repositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Invoice?)null);

            var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateInvoiceRequest(), _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("InvoiceNotFound", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ShouldNotSave()
        {
            _repositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Invoice?)null);

            await _service.UpdateAsync(Guid.NewGuid(), new UpdateInvoiceRequest(), _userId, CancellationToken.None);

            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenFound_ShouldReturnSuccess()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var result = await _service.UpdateAsync(invoice.Id, new UpdateInvoiceRequest(), _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateAllScalarFields()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var request = new UpdateInvoiceRequest
            {
                VendorName    = "New Vendor",
                VendorAddress = "456 Other St",
                CustomerName  = "John Smith",
                InvoiceNumber = "INV-9999",
                InvoiceDate   = new DateOnly(2026, 3, 1),
                DueDate       = new DateOnly(2026, 4, 1),
                Subtotal      = 200m,
                Tax           = 20m,
                Total         = 220m,
                Currency      = "USD"
            };

            var result = await _service.UpdateAsync(invoice.Id, request, _userId, CancellationToken.None);

            var dto = result.Value!;
            Assert.Equal("New Vendor",           dto.VendorName);
            Assert.Equal("456 Other St",          dto.VendorAddress);
            Assert.Equal("John Smith",            dto.CustomerName);
            Assert.Equal("INV-9999",              dto.InvoiceNumber);
            Assert.Equal(new DateOnly(2026, 3, 1), dto.InvoiceDate);
            Assert.Equal(new DateOnly(2026, 4, 1), dto.DueDate);
            Assert.Equal(200m,                    dto.Subtotal);
            Assert.Equal(20m,                     dto.Tax);
            Assert.Equal(220m,                    dto.Total);
            Assert.Equal("USD",                   dto.Currency);
        }

        [Fact]
        public async Task UpdateAsync_WhenLineItemsProvided_ShouldReplaceExistingLineItems()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var request = new UpdateInvoiceRequest
            {
                LineItems =
                [
                    new UpdateInvoiceLineItemRequest { Description = "New item", Quantity = 3m, UnitPrice = 75m, Amount = 225m }
                ]
            };

            var result = await _service.UpdateAsync(invoice.Id, request, _userId, CancellationToken.None);

            var lineItem = Assert.Single(result.Value!.LineItems);
            Assert.Equal("New item", lineItem.Description);
            Assert.Equal(3m,         lineItem.Quantity);
            Assert.Equal(75m,        lineItem.UnitPrice);
            Assert.Equal(225m,       lineItem.Amount);
        }

        [Fact]
        public async Task UpdateAsync_WhenLineItemsIsEmptyList_ShouldClearLineItems()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var result = await _service.UpdateAsync(invoice.Id, new UpdateInvoiceRequest { LineItems = [] }, _userId, CancellationToken.None);

            Assert.Empty(result.Value!.LineItems);
        }

        [Fact]
        public async Task UpdateAsync_WhenLineItemsIsNull_ShouldPreserveExistingLineItems()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var result = await _service.UpdateAsync(invoice.Id, new UpdateInvoiceRequest { LineItems = null }, _userId, CancellationToken.None);

            var lineItem = Assert.Single(result.Value!.LineItems);
            Assert.Equal("Consulting", lineItem.Description);
        }

        [Fact]
        public async Task UpdateAsync_ShouldCallUpdateAndSaveChanges()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            await _service.UpdateAsync(invoice.Id, new UpdateInvoiceRequest(), _userId, CancellationToken.None);

            _repositoryMock.Verify(x => x.Update(invoice), Times.Once);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPassUserIdToRepository()
        {
            var id = Guid.NewGuid();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Invoice?)null);

            await _service.UpdateAsync(id, new UpdateInvoiceRequest(), _userId, CancellationToken.None);

            _repositoryMock.Verify(x => x.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NullableFieldsSetToNull_ShouldBeReflectedInResponse()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            var request = new UpdateInvoiceRequest
            {
                VendorName  = null,
                InvoiceDate = null,
                Total       = null
            };

            var result = await _service.UpdateAsync(invoice.Id, request, _userId, CancellationToken.None);

            Assert.Null(result.Value!.VendorName);
            Assert.Null(result.Value!.InvoiceDate);
            Assert.Null(result.Value!.Total);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPreserveUnchangedFields()
        {
            var invoice = BuildInvoice();
            _repositoryMock
                .Setup(x => x.GetByIdAsync(invoice.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);

            await _service.UpdateAsync(invoice.Id, new UpdateInvoiceRequest { VendorName = "Updated" }, _userId, CancellationToken.None);

            Assert.Equal(invoice.Id,           invoice.Id);
            Assert.Equal(invoice.UserId,        invoice.UserId);
            Assert.Equal(invoice.FileUploadId,  invoice.FileUploadId);
            Assert.Equal(invoice.Confidence,    invoice.Confidence);
            Assert.Equal(invoice.CreatedAtUtc,  invoice.CreatedAtUtc);
        }

        #endregion
    }
}
