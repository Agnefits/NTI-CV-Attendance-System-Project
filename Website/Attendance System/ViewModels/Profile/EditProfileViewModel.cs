using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Attendance_System.ViewModels.Profile
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        public string? ImageUrl { get; set; }

        [Display(Name = "Update Profile Image")]
        public IFormFile? ImageFile { get; set; }
    }
}
