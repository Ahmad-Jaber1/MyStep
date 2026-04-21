namespace Services.Common;

public class EmbeddingOptions
{
    public string Endpoint { get; set; } = "https://router.huggingface.co/scaleway/v1/embeddings";

    public string Model { get; set; } = "qwen3-embedding-8b";

    public string ApiKey { get; set; } = string.Empty;
}