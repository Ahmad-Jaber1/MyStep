using Models;

namespace Services.Common;

public static class TaskSearchVectorBuilder
{
    public static string BuildInputText(TaskItem task, IReadOnlyDictionary<int, LearningObjective> learningObjectives)
    {
        var prerequisiteDescriptions = task.Prerequisites
            .Select(prerequisite => GetObjectiveDescription(prerequisite.LearningObjectiveId, learningObjectives))
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(description => description, StringComparer.Ordinal)
            .ToList();

        var targetDescriptions = task.Targets
            .Select(target => GetObjectiveDescription(target.LearningObjectiveId, learningObjectives))
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(description => description, StringComparer.Ordinal)
            .ToList();

        var prerequisiteText = prerequisiteDescriptions.Count == 0
            ? "none"
            : string.Join(", ", prerequisiteDescriptions);

        var targetText = targetDescriptions.Count == 0
            ? "none"
            : string.Join(", ", targetDescriptions);

        return $"This task requires the following prerequisite learning objectives: {prerequisiteText}.\n" +
               $"This task will help the learner achieve the following target learning objectives: {targetText}.";
    }

    private static string? GetObjectiveDescription(int learningObjectiveId, IReadOnlyDictionary<int, LearningObjective> learningObjectives)
    {
        if (!learningObjectives.TryGetValue(learningObjectiveId, out var learningObjective))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(learningObjective.Description)
            ? null
            : learningObjective.Description.Trim();
    }
}