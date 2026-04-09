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
    public class TaskPrerequisiteConfiguration : IEntityTypeConfiguration<TaskPrerequisite>
    {
        public void Configure(EntityTypeBuilder<TaskPrerequisite> builder)
        {
            builder.ToTable("task_prerequisites");

            builder.HasKey(tp => new { tp.TaskId, tp.LearningObjectiveId });

            builder.Property(tp => tp.Justification)
                .HasMaxLength(500);

            builder.HasOne(tp => tp.Task)
                .WithMany(t => t.Prerequisites)
                .HasForeignKey(tp => tp.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tp => tp.LearningObjective)
                .WithMany()
                .HasForeignKey(tp => tp.LearningObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
