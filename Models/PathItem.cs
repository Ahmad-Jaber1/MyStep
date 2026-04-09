using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class PathItem
    {
        public int Id { get; set; }

        public string Name { get; set; }    

        public string? Description { get; set; }

        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    }
}
