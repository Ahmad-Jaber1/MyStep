using Models;
using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskSearchVectorService
{
    Task<Result<float[]>> BuildVectorAsync(TaskItem task);
    Task<Result<TaskSearchVectorRebuildResponseDto>> RebuildAllAsync();
}