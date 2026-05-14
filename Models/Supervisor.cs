using System;
using System.Collections.Generic;

namespace Models
{
    public class Supervisor
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public int PathId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public PathItem? Path { get; set; }

        public ICollection<SupervisorStudent> SupervisorStudents { get; set; } 
            = new List<SupervisorStudent>();
    }
}
