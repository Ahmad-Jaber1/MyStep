using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Services.Common;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class HuggingFaceEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingOptions _options;

    public HuggingFaceEmbeddingClient(HttpClient httpClient, IOptions<EmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Result<float[]>> CreateEmbeddingAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Result<float[]>.Failure("Embedding API key is not configured.");
        }

        var requestPayload = new EmbeddingRequest
        {
            Input = input,
            Model = _options.Model
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
                ? $"Embedding request failed with status code {(int)response.StatusCode}."
                : $"Embedding request failed with status code {(int)response.StatusCode}: {responseBody}";

            return Result<float[]>.Failure(message);
        }

        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        var embedding = embeddingResponse?.Data?.FirstOrDefault()?.Embedding;

        if (embedding is null || embedding.Length == 0)
        {
            return Result<float[]>.Failure("Embedding API returned an empty vector.");
        }

        return Result<float[]>.Success(embedding);
    }

    private sealed class EmbeddingRequest
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingItem>? Data { get; set; }
    }

    private sealed class EmbeddingItem
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}