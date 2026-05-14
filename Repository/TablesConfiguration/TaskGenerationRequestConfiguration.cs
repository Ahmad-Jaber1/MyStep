using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration
{
    public class TaskGenerationRequestConfiguration : IEntityTypeConfiguration<TaskGenerationRequest>
    {
        public void Configure(EntityTypeBuilder<TaskGenerationRequest> builder)
        {
            builder.ToTable("task_generation_requests");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.PathId)
                .IsRequired();

            builder.Property(r => r.MainSkillId)
                .IsRequired();

            builder.Property(r => r.Status)
                .HasDefaultValue(TaskGenerationRequestStatus.Pending)
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            builder.HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Supervisor)
                .WithMany()
                .HasForeignKey(r => r.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Path)
                .WithMany()
                .HasForeignKey(r => r.PathId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
