using System;

namespace Attendance_System.ViewModels.Employee
{
    public class EmployeeListViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string Speciality { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }
}
