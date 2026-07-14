using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.AdminUser
{
    public class AdminUserFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required for new administrators")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;

        [Display(Name = "Branch")]
        public Guid? BranchId { get; set; }

        public List<SelectListItem> BranchOptions { get; set; } = new();
    }
}
