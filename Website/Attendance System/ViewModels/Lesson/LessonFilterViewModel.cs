using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Lesson
{
    public class LessonFilterViewModel
    {
        public Guid? LevelId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? TeacherId { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }

        public List<SelectListItem> LevelOptions { get; set; } = new();
        public List<SelectListItem> ClassOptions { get; set; } = new();
        public List<SelectListItem> TeacherOptions { get; set; } = new();
        public List<SelectListItem> DayOfWeekOptions { get; set; } = new();

        public List<LessonListViewModel> Results { get; set; } = new();
    }
}
