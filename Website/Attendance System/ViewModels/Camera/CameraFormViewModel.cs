using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Camera
{
    public class CameraFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Camera Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }

        [Display(Name = "Assigned Classroom")]
        public Guid? ClassRoomId { get; set; }

        [Required(ErrorMessage = "Camera Secret Key is required")]
        [StringLength(100, ErrorMessage = "Key cannot exceed 100 characters")]
        public string Key { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public List<SelectListItem> ClassRoomOptions { get; set; } = new();
    }
}
