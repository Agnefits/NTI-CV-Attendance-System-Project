using System;
using Attendance_System.Models.Enums;

namespace Attendance_System.ViewModels.Attendance
{
    public class AttendanceListViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserFullname { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // Student / Employee
        public AttendanceStatus Status { get; set; }
        public string StatusLabel => Status.ToString();
        public bool ByIA { get; set; }
        public string? CameraTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
    }
}
