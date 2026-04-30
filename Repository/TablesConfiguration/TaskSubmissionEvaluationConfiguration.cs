using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration;

public class TaskSubmissionEvaluationConfiguration : IEntityTypeConfiguration<TaskSubmissionEvaluation>
{
    public void Configure(EntityTypeBuilder<TaskSubmissionEvaluation> builder)
    {
        builder.ToTable("task_submission_evaluations");

        builder.HasKey(evaluation => evaluation.Id);

        builder.Property(evaluation => evaluation.RepositoryUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(evaluation => evaluation.Reference)
            .HasMaxLength(200);

        builder.Property(evaluation => evaluation.OverallSummary)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(evaluation => evaluation.RawModelResponseJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(evaluation => evaluation.ValidationCount)
            .IsRequired();

        builder.Property(evaluation => evaluation.PassedValidationCount)
            .IsRequired();

        builder.Property(evaluation => evaluation.FailedValidationCount)
            .IsRequired();

        builder.Property(evaluation => evaluation.CreatedAt)
            .IsRequired();

        builder.HasOne(evaluation => evaluation.Student)
            .WithMany()
            .HasForeignKey(evaluation => evaluation.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(evaluation => evaluation.Task)
            .WithMany()
            .HasForeignKey(evaluation => evaluation.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(evaluation => evaluation.ValidationResults)
            .WithOne(validation => validation.TaskSubmissionEvaluation)
            .HasForeignKey(validation => validation.TaskSubmissionEvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(evaluation => new { evaluation.StudentId, evaluation.TaskId, evaluation.CreatedAt });
    }
}