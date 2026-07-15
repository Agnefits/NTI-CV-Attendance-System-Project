using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Employee
{
    public class EmployeeFilterViewModel
    {
        public string? SearchQuery { get; set; }
        public Guid? BranchId { get; set; }

        public List<SelectListItem> BranchOptions { get; set; } = new();

        public List<EmployeeListViewModel> Results { get; set; } = new();
    }
}
