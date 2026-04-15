using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyStep;

public class Script
{
    private static readonly float[] DefaultSearchVector = new float[4096];
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public Script(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private readonly Dictionary<string, int> skills = new(StringComparer.OrdinalIgnoreCase)
    {
        { "C# Basics", 3 },
        { "OOP (Object-Oriented Programming)", 4 },
        { "LINQ & Advanced C# Concepts", 5 },
        { "ASP.NET Core Logic", 6 },
        { "EF Core (Code-First Models, DbContext, LINQ queries on DbSet)", 7 },
        { "Design Patterns & SOLID", 8 }
    };

    private readonly Dictionary<int, Dictionary<int, string>> learningObjectives = new()
    {
        [3] = new Dictionary<int, string>
        {
            [1] = "Declare and initialize variables of different primitive types (int, string, bool, double).",
            [2] = "Perform type conversions between compatible types.",
            [3] = "Use arithmetic operators (+, -, *, /, %) to perform calculations.",
            [4] = "Use relational operators (>, <, ==, !=, >=, <=) to compare values.",
            [5] = "Use logical operators (&&, ||, !) to combine boolean expressions.",
            [6] = "Use assignment operators (=, +=, -=, *=, /=, %=) to modify variables.",
            [7] = "Implement conditional statements (if, else if, else) to control program flow.",
            [8] = "Implement switch statements to handle multiple conditional cases.",
            [9] = "Implement loops: for, while, and foreach to iterate over collections.",
            [10] = "Define methods with parameters and return types.",
            [11] = "Call methods correctly and understand method scope.",
            [12] = "Apply method overloading to define multiple versions of a method.",
            [13] = "Handle exceptions using try/catch/finally blocks.",
            [14] = "Create and throw custom exceptions.",
            [15] = "Ensure proper resource cleanup in exception handling scenarios."
        },
        [4] = new Dictionary<int, string>
        {
            [1] = "Define classes with fields, properties, and methods.",
            [2] = "Create objects and access their members.",
            [3] = "Distinguish between instance and static members.",
            [4] = "Apply encapsulation using access modifiers (private, public, protected, internal).",
            [5] = "Implement inheritance to create derived classes.",
            [6] = "Override methods in derived classes to enable polymorphic behavior.",
            [7] = "Define and implement interfaces.",
            [8] = "Define and implement abstract classes.",
            [9] = "Apply composition to combine classes.",
            [10] = "Distinguish between composition and inheritance, and apply each appropriately."
        },
        [5] = new Dictionary<int, string>
        {
            [1] = "Write LINQ queries using Select to project data from collections.",
            [2] = "Write LINQ queries using Where to filter collections based on conditions.",
            [3] = "Use GroupBy in LINQ to group collection elements by a key.",
            [4] = "Use Join in LINQ to combine multiple collections.",
            [5] = "Define delegates and invoke them to reference methods dynamically.",
            [6] = "Implement events using delegates for asynchronous notifications.",
            [7] = "Create generic classes to work with type parameters.",
            [8] = "Create generic methods to handle multiple types safely.",
            [9] = "Implement asynchronous methods using async/await.",
            [10] = "Use Task and the Task Parallel Library for basic parallel operations.",
            [11] = "Apply advanced exception handling techniques, including multiple catch blocks.",
            [12] = "Create and throw custom exceptions in advanced scenarios."
        },
        [6] = new Dictionary<int, string>
        {
            [1] = "Understand the project structure of an ASP.NET Core application.",
            [2] = "Configure services in Startup.cs/Program.cs.",
            [3] = "Register and inject services using dependency injection (Scoped, Transient, Singleton).",
            [4] = "Define routes using conventional routing.",
            [5] = "Define routes using attribute routing.",
            [6] = "Implement controllers and action methods.",
            [7] = "Return appropriate IActionResult types from action methods.",
            [8] = "Bind HTTP request data to models.",
            [9] = "Validate models using data annotations.",
            [10] = "Implement custom model validation logic.",
            [11] = "Write custom middleware.",
            [12] = "Manage the request/response pipeline using Use, Run, and Map.",
            [13] = "Access and modify HTTP requests using HttpContext.",
            [14] = "Access and modify HTTP responses using HttpContext (headers, status codes).",
            [15] = "Implement action filters for cross-cutting concerns.",
            [16] = "Understand the execution order of middleware and action filters.",
            [17] = "Implement logging within controllers and middleware.",
            [18] = "Implement centralized exception handling."
        },
        [7] = new Dictionary<int, string>
        {
            [1] = "Define entity classes (Models) with properties.",
            [2] = "Define relationships between entities using navigation properties.",
            [3] = "Create a DbContext class to represent the database context.",
            [4] = "Define DbSet properties in the DbContext for each entity.",
            [5] = "Configure the DbContext in Startup.cs and register it for dependency injection.",
            [6] = "Write LINQ queries on DbSet objects to filter data.",
            [7] = "Write LINQ queries on DbSet objects to sort data.",
            [8] = "Write LINQ queries on DbSet objects to project data.",
            [9] = "Write LINQ queries on DbSet objects to join collections.",
            [10] = "Apply data annotations to configure entity properties (keys, constraints, relationships).",
            [11] = "Use Fluent API to configure entity behavior, keys, and relationships."
        },
        [8] = new Dictionary<int, string>
        {
            [1] = "Apply the Single Responsibility Principle to design classes with one responsibility.",
            [2] = "Apply the Open/Closed Principle to make classes extendable without modifying existing code.",
            [3] = "Apply the Liskov Substitution Principle to ensure derived classes can replace base classes.",
            [4] = "Apply the Interface Segregation Principle to create focused, minimal interfaces.",
            [5] = "Apply the Dependency Inversion Principle to depend on abstractions rather than concrete implementations.",
            [6] = "Implement the Repository Pattern to abstract data access logic and provide a consistent interface.",
            [7] = "Implement the Singleton Pattern to ensure a single instance of a class with thread safety.",
            [8] = "Implement the Factory Pattern to create objects without exposing instantiation logic.",
            [9] = "Implement the Strategy Pattern to define interchangeable algorithms and switch behavior dynamically."
        }
    };

    public async Task<ScriptRunResult> ImportTasksAsync(
        int pathId,
        string baseUrl,
        string tasksPath = "tasks.jsonl",
        CancellationToken cancellationToken = default)
    {
        var result = new ScriptRunResult();

        if (pathId <= 0)
        {
            result.Errors.Add("PathId must be greater than zero.");
            return result;
        }

        if (!File.Exists(tasksPath))
        {
            result.Errors.Add($"Tasks file was not found: {tasksPath}");
            return result;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));

        var objectiveCache = new Dictionary<int, Dictionary<int, int>>();
        var parsedTasks = ParseTasks(tasksPath, result);

        foreach (var payload in parsedTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.ParsedTasks++;

            if (!skills.TryGetValue(payload.SkillCategory, out var mainSkillId))
            {
                result.Errors.Add($"Task '{payload.TaskName}' skipped: unknown skill category '{payload.SkillCategory}'.");
                continue;
            }

            var taskId = await CreateTaskItemAsync(client, pathId, mainSkillId, payload, result, cancellationToken);
            if (!taskId.HasValue)
            {
                continue;
            }

            foreach (var targetLocalId in payload.TargetedObjectives.Distinct())
            {
                var objectiveId = await ResolveLearningObjectiveIdAsync(client, mainSkillId, targetLocalId, objectiveCache, cancellationToken);
                if (!objectiveId.HasValue)
                {
                    result.Errors.Add($"Task '{payload.TaskName}': targeted objective '{targetLocalId}' for skill '{payload.SkillCategory}' was not resolved.");
                    continue;
                }

                var createdTarget = await CreateTaskTargetAsync(client, taskId.Value, objectiveId.Value, result, cancellationToken);
                if (createdTarget)
                {
                    result.CreatedTargets++;
                }
            }

            foreach (var additional in payload.AdditionalSkillsRequired)
            {
                if (!skills.TryGetValue(additional.SkillName, out var additionalSkillId))
                {
                    result.Errors.Add($"Task '{payload.TaskName}': additional skill '{additional.SkillName}' was not found in mapping.");
                    continue;
                }

                var objectiveId = await ResolveLearningObjectiveIdAsync(client, additionalSkillId, additional.UsedLearningGoal, objectiveCache, cancellationToken);
                if (!objectiveId.HasValue)
                {
                    result.Errors.Add($"Task '{payload.TaskName}': prerequisite objective '{additional.UsedLearningGoal}' for skill '{additional.SkillName}' was not resolved.");
                    continue;
                }

                var createdPrerequisite = await CreateTaskPrerequisiteAsync(client, taskId.Value, objectiveId.Value, additional.Justification, result, cancellationToken);
                if (createdPrerequisite)
                {
                    result.CreatedPrerequisites++;
                }
            }
        }

        return result;
    }

    private List<TaskPayloadRecord> ParseTasks(string path, ScriptRunResult result)
    {
        var tasks = new List<TaskPayloadRecord>();
        var buffer = new StringBuilder();
        var braceCount = 0;

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            buffer.AppendLine(line);

            foreach (var ch in line)
            {
                if (ch == '{')
                {
                    braceCount++;
                }
                else if (ch == '}')
                {
                    braceCount--;
                }
            }

            if (braceCount == 0 && buffer.Length > 0)
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<TaskPayloadRecord>(buffer.ToString(), SerializerOptions);
                    if (payload is not null && !string.IsNullOrWhiteSpace(payload.TaskName))
                    {
                        tasks.Add(payload);
                    }
                }
                catch (JsonException)
                {
                    result.Errors.Add("Skipped one malformed JSON task object from tasks file.");
                }

                buffer.Clear();
            }
        }

        return tasks;
    }

    private async Task<Guid?> CreateTaskItemAsync(
        HttpClient client,
        int pathId,
        int mainSkillId,
        TaskPayloadRecord payload,
        ScriptRunResult result,
        CancellationToken cancellationToken)
    {
        var request = new CreateTaskItemApiRequest
        {
            PathId = pathId,
            MainSkillId = mainSkillId,
            TaskData = JsonSerializer.Serialize(payload),
            SearchVector = DefaultSearchVector
        };

        var response = await client.PostAsJsonAsync("/api/TaskItems", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            result.Errors.Add($"Task '{payload.TaskName}' was not created. API returned {(int)response.StatusCode}: {error}");
            return null;
        }

        var created = await response.Content.ReadFromJsonAsync<TaskItemResponseApi>(SerializerOptions, cancellationToken);
        if (created is null || created.Id == Guid.Empty)
        {
            result.Errors.Add($"Task '{payload.TaskName}' was created but response did not include a valid task id.");
            return null;
        }

        result.CreatedTasks++;
        return created.Id;
    }

    private async Task<int?> ResolveLearningObjectiveIdAsync(
        HttpClient client,
        int skillId,
        int localGoalId,
        Dictionary<int, Dictionary<int, int>> objectiveCache,
        CancellationToken cancellationToken)
    {
        if (!learningObjectives.TryGetValue(skillId, out var localObjectives) ||
            !localObjectives.TryGetValue(localGoalId, out var localDescription))
        {
            return null;
        }

        if (!objectiveCache.TryGetValue(skillId, out var mappedObjectives))
        {
            var response = await client.GetAsync($"/api/LearningObjectives/by-skill/{skillId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var apiObjectives = await response.Content.ReadFromJsonAsync<List<LearningObjectiveResponseApi>>(SerializerOptions, cancellationToken)
                ?? [];

            var byDescription = apiObjectives
                .GroupBy(x => NormalizeTextKey(x.Description))
                .ToDictionary(g => g.Key, g => g.First().Id);

            mappedObjectives = new Dictionary<int, int>();
            foreach (var objective in localObjectives)
            {
                var normalized = NormalizeTextKey(objective.Value);
                if (byDescription.TryGetValue(normalized, out var resolvedId))
                {
                    mappedObjectives[objective.Key] = resolvedId;
                }
            }

            objectiveCache[skillId] = mappedObjectives;
        }

        return mappedObjectives.TryGetValue(localGoalId, out var id) ? id : null;
    }

    private static async Task<bool> CreateTaskTargetAsync(
        HttpClient client,
        Guid taskId,
        int objectiveId,
        ScriptRunResult result,
        CancellationToken cancellationToken)
    {
        var request = new CreateTaskTargetApiRequest
        {
            TaskId = taskId,
            LearningObjectiveId = objectiveId
        };

        var response = await client.PostAsJsonAsync("/api/TaskTargets", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            result.Errors.Add($"Target was not created for task '{taskId}' and objective '{objectiveId}'. API returned {(int)response.StatusCode}: {error}");
            return false;
        }

        return true;
    }

    private static async Task<bool> CreateTaskPrerequisiteAsync(
        HttpClient client,
        Guid taskId,
        int objectiveId,
        string? justification,
        ScriptRunResult result,
        CancellationToken cancellationToken)
    {
        var request = new CreateTaskPrerequisiteApiRequest
        {
            TaskId = taskId,
            LearningObjectiveId = objectiveId,
            Justification = justification
        };

        var response = await client.PostAsJsonAsync("/api/TaskPrerequisites", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            result.Errors.Add($"Prerequisite was not created for task '{taskId}' and objective '{objectiveId}'. API returned {(int)response.StatusCode}: {error}");
            return false;
        }

        return true;
    }

    private static string NormalizeTextKey(string value)
    {
        return string.Join(' ', value
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed class CreateTaskItemApiRequest
    {
        public int PathId { get; set; }
        public int MainSkillId { get; set; }
        public string? TaskData { get; set; }
        public float[]? SearchVector { get; set; }
    }

    private sealed class CreateTaskPrerequisiteApiRequest
    {
        public Guid TaskId { get; set; }
        public int LearningObjectiveId { get; set; }
        public string? Justification { get; set; }
    }

    private sealed class CreateTaskTargetApiRequest
    {
        public Guid TaskId { get; set; }
        public int LearningObjectiveId { get; set; }
    }

    private sealed class TaskItemResponseApi
    {
        public Guid Id { get; set; }
    }

    private sealed class LearningObjectiveResponseApi
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private sealed class TaskPayloadRecord
    {
        [JsonPropertyName("task_name")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("skill_category")]
        public string SkillCategory { get; set; } = string.Empty;

        [JsonPropertyName("scenario")]
        public object? Scenario { get; set; }

        [JsonPropertyName("targeted_objectives")]
        public List<int> TargetedObjectives { get; set; } = [];

        [JsonPropertyName("additional_skills_required")]
        public List<AdditionalSkillPayloadRecord> AdditionalSkillsRequired { get; set; } = [];
    }

    private sealed class AdditionalSkillPayloadRecord
    {
        [JsonPropertyName("skill_name")]
        public string SkillName { get; set; } = string.Empty;

        [JsonPropertyName("used_learning_goal")]
        public int UsedLearningGoal { get; set; }

        [JsonPropertyName("justification")]
        public string Justification { get; set; } = string.Empty;
    }
}

public class ScriptRunResult
{
    public int ParsedTasks { get; set; }
    public int CreatedTasks { get; set; }
    public int CreatedTargets { get; set; }
    public int CreatedPrerequisites { get; set; }
    public List<string> Errors { get; set; } = [];
}

