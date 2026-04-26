using Models;
using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskSearchVectorService
{
    Task<Result<float[]>> BuildVectorAsync(TaskItem task);
    Task<Result<TaskSearchVectorRebuildResponseDto>> RebuildAllAsync();
    Task<Result<TaskGenerationPreparationResponseDto>> PrepareTaskGenerationAsync(Guid studentId, int mainSkillId);
    Task<Result<System.Text.Json.JsonDocument>> GenerateTaskAsync(Guid studentId, int mainSkillId);
}