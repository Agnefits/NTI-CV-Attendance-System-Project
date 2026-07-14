using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Class
{
    public class ClassFilterViewModel
    {
        public string? SearchQuery { get; set; }
        public Guid? LevelId { get; set; }

        public List<SelectListItem> LevelOptions { get; set; } = new();

        public List<ClassListViewModel> Results { get; set; } = new();
    }
}
