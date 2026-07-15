using System;

namespace Attendance_System.ViewModels.Level
{
    public class LevelListViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
    }
}
