using System;

namespace Attendance_System.ViewModels.ClassRoom
{
    public class ClassRoomListViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int CameraCount { get; set; }
    }
}
