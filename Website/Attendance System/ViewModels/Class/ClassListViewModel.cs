using System;

namespace Attendance_System.ViewModels.Class
{
    public class ClassListViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string LevelTitle { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }
}
