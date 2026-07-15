using System;
using Attendance_System.Models.Enums;

namespace Attendance_System.ViewModels.Attendance
{
    public class AttendanceDetailsViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserFullname { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? UserImageUrl { get; set; }
        public AttendanceStatus Status { get; set; }
        public bool ByIA { get; set; }
        public string? CameraTitle { get; set; }
        public string? CreatedByName { get; set; }
        public string? ModifiedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public string? Note { get; set; }
    }
}
