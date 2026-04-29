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
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("tasks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.TaskData)
                .IsRequired()
                .HasColumnType("jsonb");

            builder.Property(t => t.SearchVector)
                .HasColumnType("vector(1024)"); 

            
            builder.HasOne(t => t.Path)
                .WithMany()
                .HasForeignKey(t => t.PathId)
                .OnDelete(DeleteBehavior.Restrict);

           
            builder.HasOne(t => t.MainSkill)
                .WithMany()
                .HasForeignKey(t => t.MainSkillId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
