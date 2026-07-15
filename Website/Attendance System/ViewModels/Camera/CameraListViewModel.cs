using System;

namespace Attendance_System.ViewModels.Camera
{
    public class CameraListViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string ClassRoomTitle { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsOnline { get; set; } = true;
    }
}
