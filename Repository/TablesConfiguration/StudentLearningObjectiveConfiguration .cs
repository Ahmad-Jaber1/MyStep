using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration
{
    public class StudentLearningObjectiveConfiguration
        : IEntityTypeConfiguration<StudentLearningObjective>
    {
        public void Configure(EntityTypeBuilder<StudentLearningObjective> builder)
        {
            builder.ToTable("student_learning_objectives");

            builder.HasKey(slo => new { slo.StudentId, slo.LearningObjectiveId });

            builder.Property(slo => slo.StreakCount)
                .HasDefaultValue(1)
                .IsRequired();

            builder.Property(slo => slo.Score)
                .IsRequired();

            builder.Property(slo => slo.LastUpdated)
                .IsRequired();

            builder.HasOne(slo => slo.Student)
                .WithMany(s => s.StudentLearningObjectives)
                .HasForeignKey(slo => slo.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(slo => slo.LearningObjective)
                .WithMany()
                .HasForeignKey(slo => slo.LearningObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}