using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Repository;
using Services;
using Services.Interfaces;

namespace MyStep;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<Script>();
        builder.Services.AddScoped<IPathItemRepo, PathItemRepo>();
        builder.Services.AddScoped<ISkillRepo, SkillRepo>();
        builder.Services.AddScoped<ILearningObjectiveRepo, LearningObjectiveRepository>();
        builder.Services.AddScoped<ITaskItemRepo, TaskItemRepo>();
        builder.Services.AddScoped<ITaskPrerequisiteRepo, TaskPrerequisiteRepo>();
        builder.Services.AddScoped<ITaskTargetRepo, TaskTargetRepo>();
        builder.Services.AddScoped<IPathItemService, PathItemService>();
        builder.Services.AddScoped<ISkillService, SkillService>();
        builder.Services.AddScoped<ILearningObjectiveService, LearningObjectiveService>();
        builder.Services.AddScoped<ITaskItemService, TaskItemService>();
        builder.Services.AddScoped<ITaskPrerequisiteService, TaskPrerequisiteService>();
        builder.Services.AddScoped<ITaskTargetService, TaskTargetService>();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddDbContext<MyStepDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("Default"),
                o => o.UseVector()));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
