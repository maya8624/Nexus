using Nexus.Application.Dtos;
using Nexus.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Services
{
    public class AiService : IAiService
    {
        public Task<ChatResponse> GetAnswer(string message, string sessionId, CancellationToken cancellationToken = default)
        {
            var response = new ChatResponse
            {
                Answer = "AI service- test message from the backend.",
                SessionId = "test-1",
            };

            return Task.FromResult(response);

            //     var request = new AiServiceRequest
            //     {
            //         Message = message,
            //         session_id = sessionId,
            //     };

            //     _logger.LogDebug(
            //         "Calling Python AI service: session={SessionId}, message={MessagePreview}",
            //         sessionId,
            //         message[..Math.Min(80, message.Length)]);

            //     HttpResponseMessage response;

            //     try
            //     {
            //         response = await _httpClient.PostAsJsonAsync(
            //             "/chat",
            //             request,
            //             cancellationToken);
            //     }
            //     catch (HttpRequestException ex)
            //     {
            //         _logger.LogError(ex, "Failed to reach Python AI service");
            //         throw new AiServiceException("AI service is currently unavailable. Please try again.", ex);
            //     }
            //     catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            //     {
            //         _logger.LogError(ex, "Python AI service request timed out");
            //         throw new AiServiceException("AI service timed out. Please try again.", ex);
            //     }

            //     if (!response.IsSuccessStatusCode)
            //     {
            //         var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            //         _logger.LogError(
            //             "Python AI service returned {StatusCode}: {Body}",
            //             response.StatusCode,
            //             errorBody);
            //         throw new AiServiceException($"AI service error: {response.StatusCode}");
            //     }

            //     var aiResponse = await response.Content.ReadFromJsonAsync<AiServiceResponseDto>(
            //         JsonOptions,
            //         cancellationToken);

            //     if (aiResponse is null)
            //     {
            //         throw new AiServiceException("AI service returned an empty response.");
            //     }

            //     return new ChatResponseDto
            //     {
            //         Reply = aiResponse.Reply,
            //         SessionId = sessionId,
            //     };
        }

        public Task<ChatResponse> SendMessage(string message, string sessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
