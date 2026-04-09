using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Repository.TablesConfiguration
{
    public class PathItemConfiguration : IEntityTypeConfiguration<Models.PathItem>
    {
        public void Configure(EntityTypeBuilder<Models.PathItem> builder)
        {
            builder.ToTable("paths");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            // Relationship: Path → Skills (1-to-many)
            builder.HasMany(p => p.Skills)
                .WithOne(s => s.Path)
                .HasForeignKey(s => s.PathId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
