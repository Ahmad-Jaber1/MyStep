using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration
{
    public class StudentTaskConfiguration : IEntityTypeConfiguration<StudentTask>
    {
        public void Configure(EntityTypeBuilder<StudentTask> builder)
        {
            builder.ToTable("student_tasks");

            builder.HasKey(st => new { st.StudentId, st.TaskId });

            builder.HasOne(st => st.Student)
                .WithMany(s => s.StudentTasks)
                .HasForeignKey(st => st.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(st => st.Task)
                .WithMany()
                .HasForeignKey(st => st.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(st => st.NumberInMainSkill).IsRequired();
            builder.Property(st => st.Passed).HasDefaultValue(false);
        }
    }
}
