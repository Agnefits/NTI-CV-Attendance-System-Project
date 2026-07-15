using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Models.Enums;

namespace Attendance_System.ViewModels.Attendance
{
    public class StudentAttendanceFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Student selection is required")]
        [Display(Name = "Student")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "Attendance Status is required")]
        [Display(Name = "Status")]
        public AttendanceStatus Status { get; set; }

        [Display(Name = "Recorded by AI Camera")]
        public bool ByIA { get; set; }

        [Display(Name = "AI Camera")]
        public Guid? CameraId { get; set; }

        [Display(Name = "Active Lesson Slot")]
        public Guid? LessonId { get; set; }

        [StringLength(200, ErrorMessage = "Note cannot exceed 200 characters")]
        public string? Note { get; set; }

        public List<SelectListItem> StudentOptions { get; set; } = new();
        public List<SelectListItem> CameraOptions { get; set; } = new();
        public List<SelectListItem> LessonOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
    }
}
