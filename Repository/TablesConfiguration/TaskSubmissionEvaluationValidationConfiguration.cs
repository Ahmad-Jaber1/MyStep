using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration;

public class TaskSubmissionEvaluationValidationConfiguration : IEntityTypeConfiguration<TaskSubmissionEvaluationValidation>
{
    public void Configure(EntityTypeBuilder<TaskSubmissionEvaluationValidation> builder)
    {
        builder.ToTable("task_submission_evaluation_validations");

        builder.HasKey(validation => validation.Id);

        builder.Property(validation => validation.ValidationString)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(validation => validation.WhyNotPass)
            .HasMaxLength(2000);

        builder.Property(validation => validation.CreatedAt)
            .IsRequired();

        builder.HasIndex(validation => new { validation.TaskSubmissionEvaluationId, validation.ValidationId });
    }
}