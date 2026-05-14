using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;

namespace Repository.TablesConfiguration
{
    public class SupervisorConfiguration : IEntityTypeConfiguration<Supervisor>
    {
        public void Configure(EntityTypeBuilder<Supervisor> builder)
        {
            builder.ToTable("supervisors");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(s => s.Email)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(s => s.PasswordHash)
                .IsRequired();

            builder.Property(s => s.PathId)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.HasOne(s => s.Path)
                .WithMany()
                .HasForeignKey(s => s.PathId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.SupervisorStudents)
                .WithOne(ss => ss.Supervisor)
                .HasForeignKey(ss => ss.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
