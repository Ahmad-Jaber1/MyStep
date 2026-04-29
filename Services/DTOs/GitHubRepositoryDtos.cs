namespace Services.DTOs;

public class FlattenGitHubRepositoryRequestDto
{
    public string RepositoryUrl { get; set; } = string.Empty;

    public string? Ref { get; set; }
}

public class FlattenGitHubRepositoryResponseDto
{
    public string Owner { get; set; } = string.Empty;

    public string RepositoryName { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public int CodeFileCount { get; set; }

    public int SkippedFileCount { get; set; }

    public List<GitHubRepositoryFileDto> Files { get; set; } = [];

    public string FlattenedCode { get; set; } = string.Empty;
}

public class GitHubRepositoryFileDto
{
    public string Path { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}