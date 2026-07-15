using System;

namespace Attendance_System.ViewModels.Student
{
    public class StudentListViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public string LevelTitle { get; set; } = string.Empty;
        public string ClassTitle { get; set; } = string.Empty;
        public double AttendancePercent { get; set; }
    }
}
