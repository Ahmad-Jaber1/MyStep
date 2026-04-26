using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Services.Common;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class DashScopeGenerationClient : IGenerationClient
{
    private readonly HttpClient _httpClient;
    private readonly GenerationOptions _options;

    public DashScopeGenerationClient(HttpClient httpClient, IOptions<GenerationOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<string>> GenerateContentAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Result<string>.Failure("Generation API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Result<string>.Failure("Generation prompt is required.");
        }

        var requestPayload = new GenerationRequest
        {
            Model = _options.Model,
            Input = new GenerationInput
            {
                Messages = new List<GenerationMessage>
                {
                    new()
                    {
                        Role = "user",
                        Content = [new GenerationMessageContentItem { Text = prompt }]
                    }
                }
            },
            Parameters = new GenerationParameters
            {
                EnableThinking = false,
                ResultFormat = "message",
                Temperature = 0.2,
                TopP = 0.9,
                TopK = 50,
                MaxTokens = 2500,
                RepetitionPenalty = 1.1,
                PresencePenalty = 0.2,
                Seed = 42
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(requestPayload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var message = string.IsNullOrWhiteSpace(responseBody)
                ? $"Generation request failed with status code {(int)response.StatusCode}."
                : $"Generation request failed with status code {(int)response.StatusCode}: {responseBody}";

            return Result<string>.Failure(message);
        }

        var generationResponse = await response.Content.ReadFromJsonAsync<GenerationResponse>();
        var content = generationResponse?.Output?.Choices?.FirstOrDefault()?.Message?.Content?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(content))
        {
            return Result<string>.Failure("Generation API returned an empty message content.");
        }

        return Result<string>.Success(content);
    }

    private sealed class GenerationRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public GenerationInput Input { get; set; } = new();

        [JsonPropertyName("parameters")]
        public GenerationParameters Parameters { get; set; } = new();
    }

    private sealed class GenerationInput
    {
        [JsonPropertyName("messages")]
        public List<GenerationMessage> Messages { get; set; } = [];
    }

    private sealed class GenerationMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<GenerationMessageContentItem> Content { get; set; } = [];
    }

    private sealed class GenerationMessageContentItem
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GenerationParameters
    {
        [JsonPropertyName("enable_thinking")]
        public bool EnableThinking { get; set; }

        [JsonPropertyName("result_format")]
        public string ResultFormat { get; set; } = "message";

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public double TopP { get; set; }

        [JsonPropertyName("top_k")]
        public int TopK { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("repetition_penalty")]
        public double RepetitionPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public double PresencePenalty { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }
    }

    private sealed class GenerationResponse
    {
        [JsonPropertyName("output")]
        public GenerationOutput? Output { get; set; }
    }

    private sealed class GenerationOutput
    {
        [JsonPropertyName("choices")]
        public List<GenerationChoice>? Choices { get; set; }
    }

    private sealed class GenerationChoice
    {
        [JsonPropertyName("message")]
        public GenerationChoiceMessage? Message { get; set; }
    }

    private sealed class GenerationChoiceMessage
    {
        [JsonPropertyName("content")]
        public List<GenerationResponseContentItem>? Content { get; set; }
    }

    private sealed class GenerationResponseContentItem
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}