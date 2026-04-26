using Shared.Results;

namespace Services.Interfaces;

public interface IGenerationClient
{
    Task<Result<string>> GenerateContentAsync(string prompt);
}