using System;
using System.Text.Json;

namespace Services.DTOs
{
    public class CreateStudentTaskDto
    {
        public Guid StudentId { get; set; }
        public Guid TaskId { get; set; }
    }

    public class UpdateStudentTaskDto
    {
        public bool? Passed { get; set; }
        public double? Score { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class StudentTaskResponseDto
    {
        public Guid StudentId { get; set; }
        public Guid TaskId { get; set; }
        public int NumberInMainSkill { get; set; }
        public bool Passed { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double? Score { get; set; }
    }
}
