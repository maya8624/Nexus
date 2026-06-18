using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Common;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repository;
using Nexus.Domain.Enums;
using Nexus.Application.Settings;
using Nexus.Domain.ValueObjects;
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
        private readonly IEnquiryRepository _enquiryRepository;
        private readonly IUnitOfWork _uow;

        public AiService(
            IHttpClientService httpClientService,
            IHttpClientFactory httpClientFactory,
            IOptions<AiServiceSettings> settings,
            ILogger<AiService> logger,
            IUserContext userContext,
            IUserRepository userRepository,
            IEnquiryRepository enquiryRepository,
            IUnitOfWork uow
        )
        {
            _httpClientService = httpClientService;
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
            _userContext = userContext;
            _userRepository = userRepository;
            _enquiryRepository = enquiryRepository;
            _uow = uow;
        }

        public async Task<Result<CopilotResponse>> GetReply(CopilotRequest request, CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (userExists == false)
                return Result<CopilotResponse>.NotFound("UserNotFound", "User not found or inactive.");

            var isNewConversation = string.IsNullOrWhiteSpace(request.ThreadId);
            var threadId = isNewConversation
                ? Guid.NewGuid().ToString()
                : request.ThreadId;

            var aiServiceRequest = new AiCopilotRequest
            {
                message = request.Message,
                thread_id = threadId,
                property_id = request.PropertyId,
                user_id = userId,
                is_new_conversation = isNewConversation,
                metadata = new AiCopilotMetadata
                {
                    suburbs = request.Metadata?.Suburbs,
                    intent = request.Metadata?.Intent,
                    budgetMax = request.Metadata?.BudgetMax,
                    petFriendly = request.Metadata?.PetFriendly,
                    bedroomsMin = request.Metadata?.BedroomsMin,
                    bedroomsMax = request.Metadata?.BedroomsMax,
                    availableWithinDays = request.Metadata?.AvailableWithinDays,
                }
            };

            var options = BuildAiRequestOptions(aiServiceRequest, _settings.Chat);

            try
            {
                var httpRequest = HttpRequestFactory.CreateHttpRequestMessage(options);
                var result = await _httpClientService.ExecuteRequest<AiServiceResponse>(httpRequest, ct);

                return Result<CopilotResponse>.Success(new CopilotResponse
                {
                    Reply = result.reply,
                    ThreadId = result.thread_id,
                    PropertyId = result.property_id,
                    Sources = result.sources.Select(s => new SourceChunk
                    {
                        FileName = s.file_name,
                        Page = s.page,
                        Score = s.score,
                        Text = s.text
                    }).ToList(),
                    Listings = result.listings?.Select(l => new PropertyListing
                    {
                        PropertyId = l.property_id,
                        PropertyUrl = l.property_url,
                        ListingId = l.listing_Id
                    }).ToList() ?? []
                });
            }
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI suburb summary failed for suburbs {Suburbs}", string.Join(", ", request.Suburbs));
                throw new AiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }

        public async Task<Result<EnquiryDraftResponse>> GetEnquiryDraft(EnquiryDraftRequest request, CancellationToken ct)
        {
            var enquiry = await _enquiryRepository.GetByIdForUpdateAsync(request.Id, ct);
            if (enquiry is null)
                return Result<EnquiryDraftResponse>.NotFound("EnquiryNotFound", "Enquiry not found.");

            if (enquiry.Status is EnquiryStatus.Replied or EnquiryStatus.Closed)
                return Result<EnquiryDraftResponse>.Conflict("InvalidStatus", "A draft cannot be generated for a replied or closed enquiry.");

            var aiRequest = new AiEnquiryDraftRequest
            {
                id = enquiry.Id.ToString(),
                body = enquiry.Body,
                tenant_id = enquiry.TenantId?.ToString(),
                property_id = enquiry.PropertyId.ToString(),
                intent = enquiry.Intent
            };

            var options = BuildAiRequestOptions(aiRequest, _settings.EnquiryDraft);

            try
            {
                var httpRequest = HttpRequestFactory.CreateHttpRequestMessage(options);
                var raw = await _httpClientService.ExecuteRequest<AiEnquiryDraftResponse>(httpRequest, ct);

                var sources = raw.sources.Select(s => new SourceChunk
                {
                    FileName = s.file_name,
                    Page = s.page,
                    Score = s.score,
                    Text = s.text
                }).ToList();

                enquiry.Status = EnquiryStatus.Drafted;
                enquiry.DraftReply = raw.draft;
                enquiry.DraftSources = sources;
                enquiry.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await _uow.SaveChanges();

                return Result<EnquiryDraftResponse>.Success(new EnquiryDraftResponse
                {
                    Draft = raw.draft,
                    Status = EnquiryStatus.Drafted.ToString(),
                    Sources = sources
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI enquiry draft failed for enquiry {EnquiryId}", request.Id);
                throw new AiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }

        //TODO: refactor
        public async IAsyncEnumerable<string> StreamReply(
            CopilotRequest request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            Guid.TryParse(_userContext.UserId, out var userId);

            var userExists = await _userRepository.IsAny(x => x.Id == userId && x.IsActive, ct);
            if (!userExists)
                throw new AiServiceException("User not found or inactive.", null);

            var isNewConversation = string.IsNullOrWhiteSpace(request.ThreadId);
            var threadId = isNewConversation ? Guid.NewGuid().ToString() : request.ThreadId;

            var aiServiceRequest = new AiCopilotRequest
            {
                message = request.Message,
                thread_id = threadId!,
                property_id = request.PropertyId,
                user_id = userId,
                is_new_conversation = isNewConversation,
                metadata = new AiCopilotMetadata
                {
                    suburbs = request?.Metadata?.Suburbs,
                    intent = request?.Metadata?.Intent,
                    budgetMax = request?.Metadata?.BudgetMax,
                    petFriendly = request?.Metadata?.PetFriendly,
                    bedroomsMax = request?.Metadata?.BedroomsMax,
                    bedroomsMin = request?.Metadata?.BedroomsMin,
                    availableWithinDays = request?.Metadata?.AvailableWithinDays,
                }
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
            catch (Exception ex)
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

        private RequestBuilderOptions BuildAiRequestOptions(HttpContent content, string endpoint)
        {
            return new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthScheme = AuthScheme.None,
                Headers = new Dictionary<string, string>
                {
                    ["X-API-Key"] = _settings.ApiKey
                },
                Content = content,
                Url = $"{_settings.BaseUrl}/{endpoint}"
            };
        }

        public async Task<Result<DocumentIngestionResponse>> IngestDocumentAsync(byte[] fileBytes, string fileName, string? propertyId, string? docType, CancellationToken ct)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(fileBytes), "file", fileName);
            if (propertyId is not null) form.Add(new StringContent(propertyId), "property_id");
            if (docType is not null) form.Add(new StringContent(docType), "doc_type");

            var options = BuildAiRequestOptions(form, _settings.Ingestion);
            var request = HttpRequestFactory.CreateHttpRequestMessage(options);

            try
            {
                var http = _httpClientFactory.CreateClient();
                var response = await http.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var raw = await response.Content.ReadFromJsonAsync<AiIngestionResponse>(cancellationToken: ct);

                return Result<DocumentIngestionResponse>.Success(new DocumentIngestionResponse(
                    raw!.filename,
                    raw.property_id,
                    raw.doc_type,
                    raw.chunk_count,
                    raw.message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document ingestion failed for {FileName}", fileName);
                throw new AiServiceException("The AI service is currently unavailable. Please try again later.", ex);
            }
        }

        public Task<CopilotResponse> SendMessage(string message, string threadId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
