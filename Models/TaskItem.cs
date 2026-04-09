using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;

namespace Models
{
    public class TaskItem
    {
        public Guid Id { get; set; }

        public int PathId { get; set; }

        public int MainSkillId { get; set; }


        public string TaskData { get; set; } = null!;
        // JSON:
        // {
        //   "scenario": "...",
        //   "instructions": "...",
        //   "constraints": "...",
        //   "expectedOutput": "...",
        //   "evaluationCriteria": "..."
        // }

        public Vector SearchVector { get; set; } = null!;
        

        // Navigation
        public PathItem Path { get; set; } = null!;

        public Skill MainSkill { get; set; } = null!;

        public ICollection<TaskTarget> Targets { get; set; } = new List<TaskTarget>();

        public ICollection<TaskPrerequisite> Prerequisites { get; set; } = new List<TaskPrerequisite>();
    }
}
