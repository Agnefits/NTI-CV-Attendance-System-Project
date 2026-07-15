using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Employee
{
    public class EmployeeFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required for new employees")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        public string? ImageUrl { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job Title is required")]
        [StringLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Speciality is required")]
        [StringLength(100)]
        public string Speciality { get; set; } = string.Empty;

        [Display(Name = "Assigned Branch")]
        public Guid? BranchId { get; set; }

        public List<SelectListItem> BranchOptions { get; set; } = new();
    }
}
