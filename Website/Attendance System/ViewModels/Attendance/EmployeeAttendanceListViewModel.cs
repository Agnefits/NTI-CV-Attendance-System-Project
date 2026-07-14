using System;
using Attendance_System.Models.Enums;

namespace Attendance_System.ViewModels.Attendance
{
    public class EmployeeAttendanceListViewModel
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeUsername { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public AttendanceStatus Status { get; set; }
        public string StatusLabel => Status.ToString();
        public bool ByIA { get; set; }
        public string? CameraTitle { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
    }
}
