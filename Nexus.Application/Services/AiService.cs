using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Settings;
using Nexus.Network;
using Nexus.Network.Enums;
using Nexus.Network.Interfaces;

namespace Nexus.Application.Services
{
    public class AiService : IAiService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<AiService> _logger;
        private readonly AiServiceSettings _settings;
        private readonly IUserContext _userContext;
        private readonly IUserRepository _userRepository;

        public AiService(
            IHttpClientService httpClientService,
            IOptions<AiServiceSettings> settings,
            ILogger<AiService> logger,
            IUserContext userContext,
            IUserRepository userRepository
        )
        {
            _httpClientService = httpClientService;
            _settings = settings.Value;
            _logger = logger;
            _userContext = userContext;
            _userRepository = userRepository;
        }

        public async Task<Result<ChatResponse>> GetReply(ChatRequest request, CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (userExists == false)
                return Result<ChatResponse>.NotFound("UserNotFound", "User not found or inactive.");

            var isNewConversation = string.IsNullOrWhiteSpace(request.ThreadId);
            var threadId = isNewConversation
                ? Guid.NewGuid().ToString()
                : request.ThreadId;

            var aiServiceRequest = new AiServiceRequest
            {
                message = request.Message,
                thread_id = threadId,
                property_id = request.PropertyId,
                user_id = userId,
                is_new_conversation = isNewConversation
            };

            var options = new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthScheme = AuthScheme.None,
                Headers = new Dictionary<string, string>
                {
                    ["X-API-Key"] = _settings.ApiKey
                },
                Body = aiServiceRequest,
                Url = $"{_settings.BaseUrl}/api/chat/stream", //TODO: add to appsettings or make it constant
            };

            try
            {
                var httpRequest = HttpRequestFactory.CreateHttpRequestMessage(options);
                var result = await _httpClientService.ExecuteRequest<AiServiceResponse>(httpRequest, ct);

                return Result<ChatResponse>.Success(new ChatResponse
                {
                    Reply = result.reply,
                    ThreadId = result.thread_id,
                    PropertyId = result.property_id,
                    Listings = result.listings?.Select(l => new PropertyListing
                    {
                        PropertyId = l.property_id,
                        PropertyUrl = l.property_url,
                        ListingId = l.listing_Id
                    }).ToList() ?? []
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "AI service call failed for thread {ThreadId}", threadId);
                throw new AiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }

        public Task<ChatResponse> SendMessage(string message, string threadId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
