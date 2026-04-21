using Shared.Results;

namespace Services.Interfaces;

public interface IEmbeddingClient
{
    Task<Result<float[]>> CreateEmbeddingAsync(string input);
}