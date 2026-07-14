using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.AdminUser
{
    public class AdminUserFilterViewModel
    {
        public string? SearchQuery { get; set; }
        public Guid? BranchId { get; set; }

        public List<SelectListItem> BranchOptions { get; set; } = new();

        public List<AdminUserListViewModel> Results { get; set; } = new();
    }
}
