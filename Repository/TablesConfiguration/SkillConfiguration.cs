using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.TablesConfiguration
{
    public class SkillConfiguration : IEntityTypeConfiguration<Skill>
    {
        public void Configure(EntityTypeBuilder<Skill> builder)
        {
            builder.ToTable("skills");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Description)
                .HasMaxLength(1000);

            // Relationship: Skill → LearningObjectives (1-to-many)
            builder.HasMany(s => s.LearningObjectives)
                .WithOne(lo => lo.Skill)
                .HasForeignKey(lo => lo.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
