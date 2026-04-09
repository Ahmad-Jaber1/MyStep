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
    public class LearningObjectiveConfiguration : IEntityTypeConfiguration<LearningObjective>
    {
        public void Configure(EntityTypeBuilder<LearningObjective> builder)
        {
            builder.ToTable("learning_objectives");

            builder.HasKey(lo => lo.Id);

            builder.Property(lo => lo.Description)
                .IsRequired()
                .HasMaxLength(1000);
        }
    }
}
