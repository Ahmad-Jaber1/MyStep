namespace Services.Common;

public class GenerationOptions
{
    public string Endpoint { get; set; } = "https://dashscope-intl.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";

    public string Model { get; set; } = "qwen3.5-plus";

    public string ApiKey { get; set; } = string.Empty;
}