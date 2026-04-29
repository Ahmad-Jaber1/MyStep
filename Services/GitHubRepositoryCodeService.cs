using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class GitHubRepositoryCodeService : IGitHubRepositoryCodeService
{
    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".fs", ".fsx", ".vb", ".py", ".js", ".jsx", ".ts", ".tsx",
        ".java", ".kt", ".kts", ".c", ".h", ".hpp", ".hh", ".cc", ".cpp", ".cxx",
        ".m", ".mm", ".go", ".rs", ".swift", ".php", ".rb", ".lua", ".sh", ".ps1",
        ".dart", ".scala", ".sql", ".jl", ".r", ".pl"
    };

    private static readonly HashSet<string> IgnoredPathSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "bin", "obj", "node_modules", "dist", "build", "coverage", "out", "target", "vendor", "__pycache__"
    };

    private readonly HttpClient _httpClient;
    private readonly GitHubOptions _options;

    public GitHubRepositoryCodeService(HttpClient httpClient, IOptions<GitHubOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyStep.Api");
        }

        if (_httpClient.DefaultRequestHeaders.Accept.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("X-GitHub-Api-Version"))
        {
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }
    }

    public async Task<Result<FlattenGitHubRepositoryResponseDto>> FlattenRepositoryCodeAsync(string repositoryUrl, string? reference = null)
    {
        if (!TryParseRepositoryUrl(repositoryUrl, out var owner, out var repositoryName, out var urlReference))
        {
            return Result<FlattenGitHubRepositoryResponseDto>.Failure("Repository URL is invalid. Use a GitHub repository link like https://github.com/owner/repo.");
        }

        var resolvedReference = NormalizeReference(reference) ?? urlReference;
        var repoResponse = await SendGitHubRequestAsync<GitHubRepositoryResponse>(HttpMethod.Get, $"repos/{owner}/{repositoryName}");
        if (!repoResponse.IsSuccess)
        {
            return Result<FlattenGitHubRepositoryResponseDto>.Failure(repoResponse.ErrorMessage ?? "Failed to read repository metadata.");
        }

        resolvedReference ??= repoResponse.Data?.DefaultBranch;
        if (string.IsNullOrWhiteSpace(resolvedReference))
        {
            return Result<FlattenGitHubRepositoryResponseDto>.Failure("Repository reference could not be resolved.");
        }

        var treeResponse = await SendGitHubRequestAsync<GitHubTreeResponse>(HttpMethod.Get, $"repos/{owner}/{repositoryName}/git/trees/{Uri.EscapeDataString(resolvedReference)}?recursive=1");
        if (!treeResponse.IsSuccess)
        {
            return Result<FlattenGitHubRepositoryResponseDto>.Failure(treeResponse.ErrorMessage ?? "Failed to read repository tree.");
        }

        if (treeResponse.Data?.Truncated == true)
        {
            return Result<FlattenGitHubRepositoryResponseDto>.Failure("GitHub tree response was truncated, so the repository could not be fully read.");
        }

        var codeFiles = (treeResponse.Data?.Tree ?? [])
            .Where(item => item.Type == "blob")
            .Where(item => IsCodeFile(item.Path))
            .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codeFiles.Count == 0)
        {
            return Result<FlattenGitHubRepositoryResponseDto>.Failure("No code files were found in the repository.");
        }

        var fetchedFiles = new List<GitHubRepositoryFileDto>();
        var errors = new List<string>();
        var throttler = new SemaphoreSlim(4);
        var fetchTasks = codeFiles.Select(async file =>
        {
            await throttler.WaitAsync();
            try
            {
                var blobResponse = await SendGitHubRequestAsync<GitHubBlobResponse>(HttpMethod.Get, $"repos/{owner}/{repositoryName}/git/blobs/{file.Sha}");
                if (!blobResponse.IsSuccess)
                {
                    lock (errors)
                    {
                        errors.Add($"{file.Path}: {blobResponse.ErrorMessage}");
                    }
                    return;
                }

                var content = DecodeBlobContent(blobResponse.Data);
                if (content is null)
                {
                    lock (errors)
                    {
                        errors.Add($"{file.Path}: GitHub returned an empty or unsupported blob payload.");
                    }
                    return;
                }

                lock (fetchedFiles)
                {
                    fetchedFiles.Add(new GitHubRepositoryFileDto
                    {
                        Path = file.Path,
                        Content = content
                    });
                }
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(fetchTasks);

        if (errors.Count > 0)
        {
            return Result<FlattenGitHubRepositoryResponseDto>.Failure($"Failed to read one or more repository files: {string.Join(" | ", errors)}");
        }

        var orderedFiles = fetchedFiles.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase).ToList();
        var flattenedCode = BuildFlattenedCode(orderedFiles);

        var skippedCount = (treeResponse.Data?.Tree ?? [])
            .Count(item => item.Type == "blob" && !IsCodeFile(item.Path));

        return Result<FlattenGitHubRepositoryResponseDto>.Success(new FlattenGitHubRepositoryResponseDto
        {
            Owner = owner,
            RepositoryName = repositoryName,
            Reference = resolvedReference,
            CodeFileCount = orderedFiles.Count,
            SkippedFileCount = skippedCount,
            Files = orderedFiles,
            FlattenedCode = flattenedCode
        });
    }

    private async Task<Result<TResponse>> SendGitHubRequestAsync<TResponse>(HttpMethod method, string relativeUrl)
        where TResponse : class
    {
        using var request = new HttpRequestMessage(method, BuildGitHubUrl(relativeUrl));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var message = string.IsNullOrWhiteSpace(responseBody)
                ? $"GitHub request failed with status code {(int)response.StatusCode}."
                : $"GitHub request failed with status code {(int)response.StatusCode}: {responseBody}";

            return Result<TResponse>.Failure(message);
        }

        var data = await response.Content.ReadFromJsonAsync<TResponse>();
        if (data is null)
        {
            return Result<TResponse>.Failure("GitHub API returned an empty response payload.");
        }

        return Result<TResponse>.Success(data);
    }

    private string BuildGitHubUrl(string relativeUrl)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.ApiBaseUrl)
            ? "https://api.github.com"
            : _options.ApiBaseUrl.TrimEnd('/');

        return $"{baseUrl}/{relativeUrl.TrimStart('/')}";
    }

    private static string BuildFlattenedCode(IEnumerable<GitHubRepositoryFileDto> files)
    {
        var builder = new StringBuilder();

        foreach (var file in files)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine($"{file.Path}:");
            builder.AppendLine(file.Content);
        }

        return builder.ToString();
    }

    private static bool TryParseRepositoryUrl(string repositoryUrl, out string owner, out string repositoryName, out string? reference)
    {
        owner = string.Empty;
        repositoryName = string.Empty;
        reference = null;

        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return false;
        }

        if (Uri.TryCreate(repositoryUrl.Trim(), UriKind.Absolute, out var uri) &&
            uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return false;
            }

            owner = segments[0];
            repositoryName = TrimGitSuffix(segments[1]);

            if (segments.Length >= 4 && segments[2].Equals("tree", StringComparison.OrdinalIgnoreCase))
            {
                reference = Uri.UnescapeDataString(segments[3]);
            }

            return true;
        }

        const string sshPrefix = "git@github.com:";
        if (repositoryUrl.StartsWith(sshPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var path = repositoryUrl[sshPrefix.Length..].TrimEnd('/', '.').Trim();
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            owner = parts[0];
            repositoryName = TrimGitSuffix(parts[1]);
            return true;
        }

        return false;
    }

    private static string TrimGitSuffix(string value)
    {
        return value.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }

    private static string? NormalizeReference(string? reference)
    {
        return string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }

    private static bool IsCodeFile(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Any(segment => IgnoredPathSegments.Contains(segment)))
        {
            return false;
        }

        var fileName = segments.LastOrDefault() ?? string.Empty;
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return CodeExtensions.Contains(extension);
    }

    private static string? DecodeBlobContent(GitHubBlobResponse? response)
    {
        if (response is null || string.IsNullOrWhiteSpace(response.Content) || !string.Equals(response.Encoding, "base64", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var content = response.Content.Replace("\n", string.Empty).Replace("\r", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var bytes = Convert.FromBase64String(content);
        return Encoding.UTF8.GetString(bytes);
    }

    private sealed class GitHubRepositoryResponse
    {
        [JsonPropertyName("default_branch")]
        public string DefaultBranch { get; set; } = string.Empty;
    }

    private sealed class GitHubTreeResponse
    {
        [JsonPropertyName("truncated")]
        public bool Truncated { get; set; }

        [JsonPropertyName("tree")]
        public List<GitHubTreeItem> Tree { get; set; } = [];
    }

    private sealed class GitHubTreeItem
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;
    }

    private sealed class GitHubBlobResponse
    {
        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}