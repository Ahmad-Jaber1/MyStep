
using Microsoft.EntityFrameworkCore;
using Repository;
using System;
using Services;
using Services.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Pgvector.Npgsql;

namespace MyStep
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddScoped<IPathItemRepo, PathItemRepo>();
            builder.Services.AddScoped<ISkillRepo, SkillRepo>();
            builder.Services.AddScoped<ILearningObjectiveRepo, LearningObjectiveRepository>();
            builder.Services.AddScoped<IPathItemService, PathItemService>();
            builder.Services.AddScoped<ISkillService, SkillService>();
            builder.Services.AddScoped<ILearningObjectiveService, LearningObjectiveService>();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddDbContext<MyStepDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
            o=>o.UseVector()));


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
}
