using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pgvector.EntityFrameworkCore;
using Repository;
using Services;
using Services.Common;
using Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MyStep;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        

        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        builder.Services.AddSingleton(jwtOptions);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddScoped<IPathItemRepo, PathItemRepo>();
        builder.Services.AddScoped<ISkillRepo, SkillRepo>();
        builder.Services.AddScoped<ILearningObjectiveRepo, LearningObjectiveRepository>();
        builder.Services.AddScoped<ITaskItemRepo, TaskItemRepo>();
        builder.Services.AddScoped<ITaskPrerequisiteRepo, TaskPrerequisiteRepo>();
        builder.Services.AddScoped<ITaskTargetRepo, TaskTargetRepo>();
        builder.Services.AddScoped<IStudentRepo, StudentRepo>();
        builder.Services.AddScoped<IStudentLearningObjectiveRepo, StudentLearningObjectiveRepo>();
        builder.Services.AddScoped<IStudentTaskRepo, StudentTaskRepo>();
        builder.Services.AddScoped<IPathItemService, PathItemService>();
        builder.Services.AddScoped<ISkillService, SkillService>();
        builder.Services.AddScoped<ILearningObjectiveService, LearningObjectiveService>();
        builder.Services.AddScoped<ITaskItemService, TaskItemService>();
        builder.Services.AddScoped<ITaskPrerequisiteService, TaskPrerequisiteService>();
        builder.Services.AddScoped<ITaskTargetService, TaskTargetService>();
        builder.Services.AddScoped<ITaskSearchVectorService, TaskSearchVectorService>();
        builder.Services.AddScoped<IStudentService, StudentService>();
        builder.Services.AddScoped<IStudentTaskService, StudentTaskService>();
        builder.Services.AddScoped<IStudentLearningObjectiveService, StudentLearningObjectiveService>();

        builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection("Embedding"));
        builder.Services.Configure<GenerationOptions>(builder.Configuration.GetSection("Generation"));
        builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));
        builder.Services.AddHttpClient<IEmbeddingClient, HuggingFaceEmbeddingClient>();
        builder.Services.AddHttpClient<IGenerationClient, DashScopeGenerationClient>();
        builder.Services.AddHttpClient<IGitHubRepositoryCodeService, GitHubRepositoryCodeService>();

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
