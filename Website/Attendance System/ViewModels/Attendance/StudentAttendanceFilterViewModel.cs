using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Models.Enums;

namespace Attendance_System.ViewModels.Attendance
{
    public class StudentAttendanceFilterViewModel
    {
        public DateTime? Date { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? LevelId { get; set; }
        public AttendanceStatus? Status { get; set; }
        public string? SearchQuery { get; set; }

        public List<SelectListItem> ClassOptions { get; set; } = new();
        public List<SelectListItem> LevelOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<StudentAttendanceListViewModel> Results { get; set; } = new();
    }
}
