using System.Collections.Generic;

namespace Attendance_System.ViewModels.Attendance
{
    public class AttendanceReportViewModel
    {
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLate { get; set; }
        public int TotalExcused { get; set; }
        public double PresentPercentage { get; set; }
        
        public List<string> Dates { get; set; } = new();
        public List<int> DailyPresentCount { get; set; } = new();
        public List<int> DailyAbsentCount { get; set; } = new();
    }
}
