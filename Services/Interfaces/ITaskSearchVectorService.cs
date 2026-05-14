using Models;
using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskSearchVectorService
{
    Task<Result<float[]>> BuildVectorAsync(TaskItem task);
    Task<Result<TaskSearchVectorRebuildResponseDto>> RebuildAllAsync();
    Task<Result<TaskGenerationPreparationResponseDto>> PrepareTaskGenerationAsync(Guid studentId, int mainSkillId);
    Task<Result<GenerateTaskResponseDto>> GenerateTaskAsync(Guid studentId, int mainSkillId);

    // Generate a preview for supervisor review without persisting the generated task.
    Task<Result<GenerateTaskResponseDto>> GeneratePreviewAsync(Guid studentId, int mainSkillId);

    // Persist a previously-generated task JSON content for a student and main skill.
    Task<Result<Guid>> PersistGeneratedTaskFromContentAsync(Guid studentId, int mainSkillId, string generatedContent);
}