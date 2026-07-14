using System;

namespace Attendance_System.ViewModels.AdminUser
{
    public class AdminUserListViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }
}
