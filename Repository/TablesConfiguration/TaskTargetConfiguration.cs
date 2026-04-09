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
    public class TaskTargetConfiguration : IEntityTypeConfiguration<TaskTarget>
    {
        public void Configure(EntityTypeBuilder<TaskTarget> builder)
        {
            builder.ToTable("task_targets");

            builder.HasKey(tt => new { tt.TaskId, tt.LearningObjectiveId });

            builder.HasOne(tt => tt.Task)
                .WithMany(t => t.Targets)
                .HasForeignKey(tt => tt.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tt => tt.LearningObjective)
                .WithMany()
                .HasForeignKey(tt => tt.LearningObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
