using Nexus.Application.Common;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Application.Services
{
    public class EnquiryService : IEnquiryService
    {
        private readonly IEnquiryRepository _enquiryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _uow;

        public EnquiryService(
            IEnquiryRepository enquiryRepository,
            IUserRepository userRepository,
            IUnitOfWork uow)
        {
            _enquiryRepository = enquiryRepository;
            _userRepository = userRepository;
            _uow = uow;
        }

        public async Task<Result<EnquiryResponse>> CreateAsync(CreateEnquiryRequest request, Guid userId, CancellationToken ct)
        {
            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (userExists == false)
                return Result<EnquiryResponse>.NotFound("UserNotFound", "User not found or inactive.");

            var now = DateTimeOffset.UtcNow;
            var enquiry = new Enquiry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PropertyId = request.PropertyId,
                ListingId = request.ListingId,
                AgentId = request.AgentId,
                Body = request.Body.Trim(),
                Status = EnquiryStatus.New,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _enquiryRepository.Create(enquiry, ct);
            await _uow.SaveChanges();

            return Result<EnquiryResponse>.Success(MapToDto(enquiry));
        }

        public async Task<Result<IReadOnlyList<EnquiryResponse>>> GetMyEnquiriesAsync(Guid userId, CancellationToken ct)
        {
            var enquiries = await _enquiryRepository.GetByUserIdAsync(userId, ct);
            return Result<IReadOnlyList<EnquiryResponse>>.Success(enquiries.Select(MapToDto).ToList());
        }

        public async Task<Result<EnquiryResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
        {
            var enquiry = await _enquiryRepository.GetByIdAsync(id, userId, ct);
            if (enquiry is null)
                return Result<EnquiryResponse>.NotFound("EnquiryNotFound", "Enquiry not found.");

            return Result<EnquiryResponse>.Success(MapToDto(enquiry));
        }

        public async Task<Result<EnquiryResponse>> UpdateAsync(Guid id, UpdateEnquiryRequest request, Guid userId, CancellationToken ct)
        {
            var enquiry = await _enquiryRepository.GetByIdForUpdateAsync(id, userId, ct);
            if (enquiry is null)
                return Result<EnquiryResponse>.NotFound("EnquiryNotFound", "Enquiry not found.");

            if (enquiry.Status != EnquiryStatus.New)
                return Result<EnquiryResponse>.Conflict("InvalidStatus", "Only new enquiries can be updated.");

            enquiry.Body = request.Body.Trim();
            enquiry.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _uow.SaveChanges();

            return Result<EnquiryResponse>.Success(MapToDto(enquiry));
        }

        public async Task<Result<IReadOnlyList<EnquiryResponse>>> GetByAgentIdAsync(Guid agentId, CancellationToken ct)
        {
            var enquiries = await _enquiryRepository.GetByAgentIdAsync(agentId, ct);
            return Result<IReadOnlyList<EnquiryResponse>>.Success(enquiries.Select(MapToDto).ToList());
        }

        public async Task<Result<EnquirySendResponse>> SendReplyAsync(Guid id, CancellationToken ct)
        {
            var enquiry = await _enquiryRepository.GetByIdForUpdateAsync(id, ct);
            if (enquiry is null)
                return Result<EnquirySendResponse>.NotFound("EnquiryNotFound", "Enquiry not found.");

            if (string.IsNullOrWhiteSpace(enquiry.DraftReply))
                return Result<EnquirySendResponse>.Conflict("NoDraft", "No draft reply exists to send.");

            if (enquiry.Status is EnquiryStatus.Replied or EnquiryStatus.Closed)
                return Result<EnquirySendResponse>.Conflict("InvalidStatus", "Enquiry has already been replied to or closed.");

            var now = DateTimeOffset.UtcNow;
            enquiry.SentReply = enquiry.DraftReply;
            enquiry.Status = EnquiryStatus.Replied;
            enquiry.RepliedAtUtc = now;
            enquiry.UpdatedAtUtc = now;
            await _uow.SaveChanges();

            //TODO: implement sending an email

            return Result<EnquirySendResponse>.Success(new EnquirySendResponse
            {
                SentReply    = enquiry.SentReply,
                RepliedAtUtc = now
            });
        }

        private static EnquiryResponse MapToDto(Enquiry enquiry) => new()
        {
            Id = enquiry.Id,
            PropertyId = enquiry.PropertyId,
            ListingId = enquiry.ListingId,
            AgentId = enquiry.AgentId,
            Body = enquiry.Body,
            DraftReply = enquiry.DraftReply,
            SentReply = enquiry.SentReply,
            Status = enquiry.Status.ToString(),
            SenderName = $"{enquiry.User?.FirstName} {enquiry.User?.LastName}".Trim(),
            SenderEmail = enquiry.User?.Email ?? string.Empty,
            CreatedAtUtc = enquiry.CreatedAtUtc,
            RepliedAtUtc = enquiry.RepliedAtUtc
        };
    }
}
