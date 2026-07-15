using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Student
{
    public class StudentFilterViewModel
    {
        public string? SearchQuery { get; set; }
        public Guid? LevelId { get; set; }
        public Guid? ClassId { get; set; }

        public List<SelectListItem> LevelOptions { get; set; } = new();
        public List<SelectListItem> ClassOptions { get; set; } = new();

        public List<StudentListViewModel> Results { get; set; } = new();
    }
}
