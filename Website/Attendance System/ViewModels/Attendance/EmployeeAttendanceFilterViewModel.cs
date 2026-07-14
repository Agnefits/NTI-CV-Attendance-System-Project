using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Models.Enums;

namespace Attendance_System.ViewModels.Attendance
{
    public class EmployeeAttendanceFilterViewModel
    {
        public DateTime? Date { get; set; }
        public Guid? BranchId { get; set; }
        public AttendanceStatus? Status { get; set; }
        public string? SearchQuery { get; set; }

        public List<SelectListItem> BranchOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<EmployeeAttendanceListViewModel> Results { get; set; } = new();
    }
}
