using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;

namespace Repository
{
    public class MyStepDbContext : DbContext
    {
        public MyStepDbContext(DbContextOptions<MyStepDbContext> options)
        : base(options)
        {
        }
        public DbSet<PathItem> Paths => Set<PathItem>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<LearningObjective> LearningObjectives => Set<LearningObjective>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<TaskTarget> TaskTargets => Set<TaskTarget>();
        public DbSet<TaskPrerequisite> TaskPrerequisites => Set<TaskPrerequisite>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<StudentLearningObjective> StudentLearningObjectives => Set<StudentLearningObjective>();
        public DbSet<StudentTask> StudentTasks => Set<StudentTask>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");
            

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyStepDbContext).Assembly);
        }
    }
}
