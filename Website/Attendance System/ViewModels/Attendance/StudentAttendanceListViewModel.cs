using System;
using Attendance_System.Models.Enums;

namespace Attendance_System.ViewModels.Attendance
{
    public class StudentAttendanceListViewModel
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentUsername { get; set; } = string.Empty;
        public string ClassTitle { get; set; } = string.Empty;
        public string LevelTitle { get; set; } = string.Empty;

        public AttendanceStatus Status { get; set; }
        public string StatusLabel => Status.ToString();
        public bool ByIA { get; set; }
        public string? CameraTitle { get; set; }

        public Guid? LessonId { get; set; }
        public string LessonTimeSlot { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
    }
}
