using System;

namespace Attendance_System.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
