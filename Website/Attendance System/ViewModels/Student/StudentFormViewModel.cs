using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Student
{
    public class StudentFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required for new students")]
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

        [Required(ErrorMessage = "Academic Level is required")]
        [Display(Name = "Level")]
        public Guid LevelId { get; set; }

        [Display(Name = "Class")]
        public Guid? ClassId { get; set; }

        public List<SelectListItem> LevelOptions { get; set; } = new();
        public List<SelectListItem> ClassOptions { get; set; } = new();
    }
}
