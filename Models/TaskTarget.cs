using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class TaskTarget
    {
        public Guid TaskId { get; set; }

        public int LearningObjectiveId { get; set; }


        public TaskItem Task { get; set; } = null!;

        public LearningObjective LearningObjective { get; set; } = null!;
    }
}
