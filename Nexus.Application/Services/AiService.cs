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
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Nexus.Application.Services
{
    public class AiService : IAiService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AiService> _logger;
        private readonly AiServiceSettings _settings;
        private readonly IUserContext _userContext;
        private readonly IUserRepository _userRepository;

        public AiService(
            IHttpClientService httpClientService,
            IHttpClientFactory httpClientFactory,
            IOptions<AiServiceSettings> settings,
            ILogger<AiService> logger,
            IUserContext userContext,
            IUserRepository userRepository
        )
        {
            _httpClientService = httpClientService;
            _httpClientFactory = httpClientFactory;
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

            var options = BuildAiRequestOptions(aiServiceRequest, _settings.Chat);

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

        public async Task<Result<PreferenceSearchResponse>> GetPreferenceProperties(TenantPreferenceRequest request, Guid userId, CancellationToken ct)
        {
            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (userExists == false)
                return Result<PreferenceSearchResponse>.NotFound("UserNotFound", "User not found or inactive.");

            var aiRequest = new TenantPreferenceAiRequest
            {
                suburbs = request.Suburbs,
                maxRent = request.MaxRent,
                minBeds = request.MinBeds,
                maxBeds = request.MaxBeds,
                petFriendly = request.PetFriendly,
                availableWithinDays = request.AvailableWithinDays
            };

            var options = BuildAiRequestOptions(aiRequest, _settings.Preferences);

            try
            {
                var httpRequest = HttpRequestFactory.CreateHttpRequestMessage(options);
                var raw = await _httpClientService.ExecuteRequest<AiTenantPreferenceSearchResponse>(httpRequest, ct);

                var response = new PreferenceSearchResponse
                {
                    Message = raw.Message,
                    DisplayCount = raw.DisplayCount,
                    TotalCount = raw.TotalCount,
                    HasMore = raw.HasMore,
                    Listings = raw.Listings.Select(l => new ListingItem
                    {
                        PropertyId = l.PropertyId,
                        ListingId = l.ListingId,
                        ImageUrl = l.ImageUrl,
                        AddressLine1 = l.AddressLine1,
                        Suburb = l.Suburb,
                        Bedrooms = l.Bedrooms,
                        Bathrooms = l.Bathrooms,
                        Price = l.Price,
                        BuildingSizeSqm = l.BuildingSizeSqm,
                        PropertyType = l.PropertyType
                    }).ToList()
                };

                return Result<PreferenceSearchResponse>.Success(response);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "AI preference search failed for user {UserId}", userId);
                throw new AiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }


        public async Task<Result<SuburbSummaryResponse>> GetSuburbSummary(SuburbSummaryRequest request, Guid userId, CancellationToken ct)
        {
            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (userExists == false)
                return Result<SuburbSummaryResponse>.NotFound("UserNotFound", "User not found or inactive.");

            var aiRequest = new { suburbs = request.Suburbs };
            var options = BuildAiRequestOptions(aiRequest, _settings.SuburbSummary);

            try
            {
                var httpRequest = HttpRequestFactory.CreateHttpRequestMessage(options);
                var raw = await _httpClientService.ExecuteRequest<AiSuburbSummaryResponse>(httpRequest, ct);

                return Result<SuburbSummaryResponse>.Success(new SuburbSummaryResponse
                {
                    Suburbs = raw.Suburbs.Select(s => new SuburbProfile
                    {
                        Name = s.Name,
                        Description = s.Description,
                        Rents = new SuburbRents
                        {
                            OneBedroom = s.Rents.OneBedroom,
                            TwoBedroom = s.Rents.TwoBedroom,
                            ThreeBedroom = s.Rents.ThreeBedroom
                        },
                        VacancyRate = s.VacancyRate,
                        Trend = s.Trend
                    }).ToList()
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "AI suburb summary failed for suburbs {Suburbs}", string.Join(", ", request.Suburbs));
                throw new AiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }

        //TODO: refactor
        public async IAsyncEnumerable<string> StreamReply(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (!userExists)
                throw new AiServiceException("User not found or inactive.", null);

            var isNewConversation = string.IsNullOrWhiteSpace(request.ThreadId);
            var threadId = isNewConversation ? Guid.NewGuid().ToString() : request.ThreadId;

            var aiServiceRequest = new AiServiceRequest
            {
                message = request.Message,
                thread_id = threadId!,
                property_id = request.PropertyId,
                user_id = userId,
                is_new_conversation = isNewConversation
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/{_settings.ChatStream}")
            {
                Content = JsonContent.Create(aiServiceRequest)
            };
            httpRequest.Headers.Add("X-API-Key", _settings.ApiKey);

            var http = _httpClientFactory.CreateClient();
            HttpResponseMessage response;

            try
            {
                response = await http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "AI stream request failed for thread {ThreadId}", threadId);
                throw new AiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;

                if (!line.StartsWith("data:")) continue;

                var chunk = line["data:".Length..].Trim();
                if (chunk == "[DONE]") break;
                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk;
            }
        }

        private RequestBuilderOptions BuildAiRequestOptions(object body, string endpoint)
        {
            return new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthScheme = AuthScheme.None,
                Headers = new Dictionary<string, string>
                {
                    ["X-API-Key"] = _settings.ApiKey
                },
                Body = body,
                Url = $"{_settings.BaseUrl}/{endpoint}"
            };
        }

        public Task<ChatResponse> SendMessage(string message, string threadId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
