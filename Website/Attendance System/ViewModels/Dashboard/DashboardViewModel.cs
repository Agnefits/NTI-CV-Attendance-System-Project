using System.Collections.Generic;
using Attendance_System.ViewModels.Attendance;

namespace Attendance_System.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public int TotalCameras { get; set; }
        public List<AttendanceListViewModel> RecentAttendances { get; set; } = new();

        // Scoped / Role-specific attributes
        public double AttendanceRate { get; set; }
        public int TotalLessonsOrDays { get; set; }
        public int AttendedCount { get; set; }
        public string Role { get; set; } = "Admin";
    }
}
