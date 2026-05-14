using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration
{
    public class SupervisorStudentConfiguration : IEntityTypeConfiguration<SupervisorStudent>
    {
        public void Configure(EntityTypeBuilder<SupervisorStudent> builder)
        {
            builder.ToTable("supervisor_students");

            builder.HasKey(ss => new { ss.SupervisorId, ss.StudentId, ss.PathId });

            builder.Property(ss => ss.Status)
                .HasDefaultValue(ApprovalStatus.Pending)
                .IsRequired();

            builder.Property(ss => ss.CreatedAt)
                .IsRequired();

            builder.HasOne(ss => ss.Supervisor)
                .WithMany(s => s.SupervisorStudents)
                .HasForeignKey(ss => ss.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ss => ss.Student)
                .WithMany(s => s.SupervisorStudents)
                .HasForeignKey(ss => ss.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ss => ss.Path)
                .WithMany()
                .HasForeignKey(ss => ss.PathId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
