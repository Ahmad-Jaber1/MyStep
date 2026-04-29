namespace Services.Common;

public class GitHubOptions
{
    public string ApiBaseUrl { get; set; } = "https://api.github.com";

    public string ApiKey { get; set; } = string.Empty;
}