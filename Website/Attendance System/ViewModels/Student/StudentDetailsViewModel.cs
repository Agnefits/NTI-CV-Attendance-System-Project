using System;
using System.Collections.Generic;
using Attendance_System.ViewModels.Attendance;

namespace Attendance_System.ViewModels.Student
{
    public class StudentDetailsViewModel
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
        public bool HasFaceEmbedding { get; set; }
        public List<AttendanceListViewModel> RecentAttendances { get; set; } = new();
    }
}
