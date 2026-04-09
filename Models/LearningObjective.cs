using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class LearningObjective
    {
        public int Id { get; set; }

        public int SkillId { get; set; }

        public string Description { get; set; } = null!;

        
        public Skill Skill { get; set; } = null!;
    }
}
