using Moq;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Services;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using System.Linq.Expressions;
using Xunit;

namespace Nexus.Tests.Unit.Application
{
    [Trait("Category", "Unit")]
    public class EnquiryServiceTests
    {
        private readonly Mock<IEnquiryRepository> _enquiryRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly EnquiryService _service;
        private readonly Guid _userId = Guid.NewGuid();

        public EnquiryServiceTests()
        {
            _uowMock.Setup(x => x.SaveChanges()).ReturnsAsync(1);
            _service = new EnquiryService(
                _enquiryRepositoryMock.Object,
                _userRepositoryMock.Object,
                _uowMock.Object);
        }

        private void SetupUserExists(bool exists) =>
            _userRepositoryMock
                .Setup(x => x.IsAny(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);

        private static Enquiry BuildEnquiry(Guid? userId = null, EnquiryStatus status = EnquiryStatus.New)
        {
            var id = userId ?? Guid.NewGuid();
            return new()
            {
                Id         = Guid.NewGuid(),
                UserId     = id,
                PropertyId = Guid.NewGuid(),
                AgentId    = Guid.NewGuid(),
                Body       = "Is the property still available?",
                Status     = status,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                User = new User
                {
                    Id        = id,
                    FirstName = "Sarah",
                    LastName  = "Chen",
                    Email     = "sarah@example.com",
                    IsActive  = true
                }
            };
        }

        private static CreateEnquiryRequest BuildCreateRequest() => new()
        {
            PropertyId = Guid.NewGuid(),
            AgentId    = Guid.NewGuid(),
            Body       = "  Is the property still available?  "
        };

        #region CreateAsync

        [Fact]
        public async Task CreateAsync_WithMissingUser_ShouldReturnNotFound()
        {
            SetupUserExists(false);

            var result = await _service.CreateAsync(BuildCreateRequest(), _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("UserNotFound", Assert.Single(result.Errors).Code);
            _enquiryRepositoryMock.Verify(x => x.Create(It.IsAny<Enquiry>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithValidUser_ShouldReturnCreatedEnquiry()
        {
            SetupUserExists(true);
            _enquiryRepositoryMock.Setup(x => x.Create(It.IsAny<Enquiry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(BuildCreateRequest(), _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Is the property still available?", result.Value!.Body);
        }

        [Fact]
        public async Task CreateAsync_ShouldTrimBody()
        {
            SetupUserExists(true);
            _enquiryRepositoryMock.Setup(x => x.Create(It.IsAny<Enquiry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(BuildCreateRequest(), _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Is the property still available?", result.Value!.Body);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetStatusToNew()
        {
            SetupUserExists(true);
            Enquiry? captured = null;
            _enquiryRepositoryMock
                .Setup(x => x.Create(It.IsAny<Enquiry>(), It.IsAny<CancellationToken>()))
                .Callback<Enquiry, CancellationToken>((e, _) => captured = e)
                .Returns(Task.CompletedTask);

            await _service.CreateAsync(BuildCreateRequest(), _userId, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.Equal(EnquiryStatus.New, captured!.Status);
        }

        [Fact]
        public async Task CreateAsync_ShouldCallSaveChanges()
        {
            SetupUserExists(true);
            _enquiryRepositoryMock.Setup(x => x.Create(It.IsAny<Enquiry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await _service.CreateAsync(BuildCreateRequest(), _userId, CancellationToken.None);

            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        #endregion

        #region GetMyEnquiriesAsync

        [Fact]
        public async Task GetMyEnquiriesAsync_ShouldReturnEnquiriesForUser()
        {
            var enquiries = new List<Enquiry> { BuildEnquiry(_userId), BuildEnquiry(_userId) };
            _enquiryRepositoryMock
                .Setup(x => x.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enquiries);

            var result = await _service.GetMyEnquiriesAsync(_userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task GetMyEnquiriesAsync_WithNoEnquiries_ShouldReturnEmptyList()
        {
            _enquiryRepositoryMock
                .Setup(x => x.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enquiry>());

            var result = await _service.GetMyEnquiriesAsync(_userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        #endregion

        #region GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldReturnEnquiry()
        {
            var enquiry = BuildEnquiry(_userId);
            _enquiryRepositoryMock
                .Setup(x => x.GetByIdAsync(enquiry.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enquiry);

            var result = await _service.GetByIdAsync(enquiry.Id, _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(enquiry.Id, result.Value!.Id);
            Assert.Equal("Sarah Chen", result.Value.SenderName);
            Assert.Equal("sarah@example.com", result.Value.SenderEmail);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ShouldReturnNotFound()
        {
            _enquiryRepositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Enquiry?)null);

            var result = await _service.GetByIdAsync(Guid.NewGuid(), _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("EnquiryNotFound", Assert.Single(result.Errors).Code);
        }

        #endregion

        #region UpdateAsync

        [Fact]
        public async Task UpdateAsync_WhenNotFound_ShouldReturnNotFound()
        {
            _enquiryRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Enquiry?)null);

            var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateEnquiryRequest { Body = "Updated" }, _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("EnquiryNotFound", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public async Task UpdateAsync_WhenStatusIsNotNew_ShouldReturnConflict()
        {
            var enquiry = BuildEnquiry(_userId, EnquiryStatus.Replied);
            _enquiryRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(enquiry.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enquiry);

            var result = await _service.UpdateAsync(enquiry.Id, new UpdateEnquiryRequest { Body = "Updated" }, _userId, CancellationToken.None);

            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal("InvalidStatus", Assert.Single(result.Errors).Code);
            _uowMock.Verify(x => x.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WithNewEnquiry_ShouldUpdateBodyAndSave()
        {
            var enquiry = BuildEnquiry(_userId, EnquiryStatus.New);
            _enquiryRepositoryMock
                .Setup(x => x.GetByIdForUpdateAsync(enquiry.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enquiry);

            var result = await _service.UpdateAsync(enquiry.Id, new UpdateEnquiryRequest { Body = "  Updated body  " }, _userId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Updated body", result.Value!.Body);
            _uowMock.Verify(x => x.SaveChanges(), Times.Once);
        }

        #endregion

        #region GetByAgentIdAsync

        [Fact]
        public async Task GetByAgentIdAsync_ShouldReturnEnquiriesForAgent()
        {
            var agentId = Guid.NewGuid();
            var enquiries = new List<Enquiry> { BuildEnquiry(), BuildEnquiry(), BuildEnquiry() };
            _enquiryRepositoryMock
                .Setup(x => x.GetByAgentIdAsync(agentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enquiries);

            var result = await _service.GetByAgentIdAsync(agentId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value!.Count);
        }

        [Fact]
        public async Task GetByAgentIdAsync_WithNoEnquiries_ShouldReturnEmptyList()
        {
            var agentId = Guid.NewGuid();
            _enquiryRepositoryMock
                .Setup(x => x.GetByAgentIdAsync(agentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enquiry>());

            var result = await _service.GetByAgentIdAsync(agentId, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        #endregion
    }
}
