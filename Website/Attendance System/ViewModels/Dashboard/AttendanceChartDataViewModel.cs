using System.Collections.Generic;

namespace Attendance_System.ViewModels.Dashboard
{
    public class AttendanceChartDataViewModel
    {
        public List<string> Labels { get; set; } = new();
        public List<int> PresentData { get; set; } = new();
        public List<int> AbsentData { get; set; } = new();
        public List<int> LateData { get; set; } = new();
    }
}
