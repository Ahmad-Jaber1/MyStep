using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("students");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(s => s.Email)
                .HasMaxLength(200)
                .IsRequired();

           

            builder.Property(s => s.PasswordHash)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.HasCompletedWelcomeAssessment)
                .HasDefaultValue(false)
                .IsRequired();

            builder.HasOne(s => s.SelectedPath)
                .WithMany()
                .HasForeignKey(s => s.SelectedPathId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}