using System;

namespace Attendance_System.ViewModels.Lesson
{
    public class LessonListViewModel
    {
        public Guid Id { get; set; }
        public Guid LevelId { get; set; }
        public string LevelTitle { get; set; } = string.Empty;
        public Guid? ClassId { get; set; }
        public string ClassTitle { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public Guid? ClassRoomId { get; set; }
        public string ClassRoomTitle { get; set; } = string.Empty;

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DayOfWeek DayOfWeek { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string StartTimeFormatted => DateTime.Today.Add(StartTime).ToString("hh:mm tt");
        public string EndTimeFormatted => DateTime.Today.Add(EndTime).ToString("hh:mm tt");
        public string DayOfWeekName => DayOfWeek.ToString();
    }
}
