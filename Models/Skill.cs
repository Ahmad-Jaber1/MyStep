using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Skill
    {
        public int Id { get; set; }

        public int PathId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }


        public PathItem Path { get; set; } = null!;

        public ICollection<LearningObjective> LearningObjectives { get; set; } = new List<LearningObjective>();
    }
}
