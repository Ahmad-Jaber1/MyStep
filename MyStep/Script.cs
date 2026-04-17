using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Services.DTOs;

public class Script
{
    private readonly Dictionary<string, int> skills = new()
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

    // ✅ Normalize helper (important for matching text safely)
    private string Normalize(string text)
        => text.Trim().ToLowerInvariant();

    public async Task ReadTask(string path = "tasks.jsonl")
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return;
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7147/")
        };

        // ============================================
        // ✅ STEP 1: LOAD DB OBJECTIVES
        // ============================================
        var dbObjectives = await client.GetFromJsonAsync<List<LearningObjectiveResponseDto>>("api/learningobjectives");

        var dbObjectivesByText = new Dictionary<string, int>();

        foreach (var obj in dbObjectives!)
        {
            var key = Normalize(obj.Description);
            if (!dbObjectivesByText.ContainsKey(key))
                dbObjectivesByText[key] = obj.Id;
        }

        Console.WriteLine($"Loaded {dbObjectivesByText.Count} learning objectives from DB");

        // ============================================
        var buffer = new StringBuilder();
        var braceCount = 0;

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            buffer.AppendLine(line);

            foreach (var ch in line)
            {
                if (ch == '{') braceCount++;
                if (ch == '}') braceCount--;
            }

            if (braceCount != 0)
                continue;

            var json = buffer.ToString();

            TaskPayload? task;
            try
            {
                task = JsonSerializer.Deserialize<TaskPayload>(json);
            }
            catch
            {
                buffer.Clear();
                continue;
            }

            if (task == null)
            {
                buffer.Clear();
                continue;
            }

            if (!skills.TryGetValue(task.SkillCategory, out var skillId))
            {
                Console.WriteLine($"Unknown skill: {task.SkillCategory}");
                buffer.Clear();
                continue;
            }

            // ============================================
            // ✅ CREATE TASK ITEM
            // ============================================
            using var taskDataDocument = JsonDocument.Parse(json);

            var createDto = new CreateTaskItemDto
            {
                PathId = 5,
                MainSkillId = skillId,
                TaskData = taskDataDocument,
                SearchVector = new float[4096]
            };

            var taskResponse = await client.PostAsJsonAsync("api/taskitems", createDto);
            taskResponse.EnsureSuccessStatusCode();

            var taskResponseJson = await taskResponse.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(taskResponseJson);

            Guid taskId =
                doc.RootElement.TryGetProperty("id", out var idProp)
                    ? idProp.GetGuid()
                    : doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();

            Console.WriteLine($"Created Task: {task.TaskName}");

            // ============================================
            // ✅ TARGET OBJECTIVES (FIXED)
            // ============================================
            foreach (var localObjectiveId in task.TargetedObjectives)
            {
                if (!learningObjectives.ContainsKey(skillId) ||
                    !learningObjectives[skillId].TryGetValue(localObjectiveId, out var text))
                {
                    Console.WriteLine($"Local objective not found: {localObjectiveId}");
                    continue;
                }

                var normalized = Normalize(text);

                if (!dbObjectivesByText.TryGetValue(normalized, out var dbId))
                {
                    Console.WriteLine($"❌ NOT FOUND IN DB: {text}");
                    continue;
                }

                var targetDto = new CreateTaskTargetDto
                {
                    TaskId = taskId,
                    LearningObjectiveId = dbId
                };

                await client.PostAsJsonAsync("api/tasktargets", targetDto);

                Console.WriteLine($"✔ Linked Local {localObjectiveId} → DB {dbId}");
            }

            // ============================================
            // ✅ PREREQUISITES (FIXED)
            // ============================================
            foreach (var additional in task.AdditionalSkillsRequired)
            {
                if (!skills.TryGetValue(additional.SkillName, out var additionalSkillId))
                    continue;

                if (!learningObjectives.ContainsKey(additionalSkillId) ||
                    !learningObjectives[additionalSkillId]
                        .TryGetValue(additional.UsedLearningGoal, out var text))
                    continue;

                var normalized = Normalize(text);

                if (!dbObjectivesByText.TryGetValue(normalized, out var dbId))
                {
                    Console.WriteLine($"❌ PREREQ NOT FOUND: {text}");
                    continue;
                }

                var prerequisiteDto = new CreateTaskPrerequisiteDto
                {
                    TaskId = taskId,
                    LearningObjectiveId = dbId,
                    Justification = additional.Justification
                };

                await client.PostAsJsonAsync("api/taskprerequisites", prerequisiteDto);
            }

            Console.WriteLine("------------------------------");

            buffer.Clear();
        }
    }
}
