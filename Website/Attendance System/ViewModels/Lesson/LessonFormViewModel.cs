using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Lesson
{
    public class LessonFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Level is required")]
        [Display(Name = "Academic Level")]
        public Guid LevelId { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }

        [Required(ErrorMessage = "Teacher is required")]
        [Display(Name = "Teacher / Instructor")]
        public Guid TeacherId { get; set; }

        [Display(Name = "Classroom")]
        public Guid? ClassRoomId { get; set; }

        [Required(ErrorMessage = "Start Time is required")]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End Time is required")]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Day of Week is required")]
        [Display(Name = "Day of Week")]
        public DayOfWeek DayOfWeek { get; set; }

        [Display(Name = "Start Date (Optional)")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date (Optional)")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public List<SelectListItem> LevelOptions { get; set; } = new();
        public List<SelectListItem> ClassOptions { get; set; } = new();
        public List<SelectListItem> TeacherOptions { get; set; } = new();
        public List<SelectListItem> ClassRoomOptions { get; set; } = new();
        public List<SelectListItem> DayOfWeekOptions { get; set; } = new();
    }
}
