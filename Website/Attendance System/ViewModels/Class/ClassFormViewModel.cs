using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attendance_System.ViewModels.Class
{
    public class ClassFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Class Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Level assignment is required")]
        [Display(Name = "Academic Level")]
        public Guid LevelId { get; set; }

        public List<SelectListItem> LevelOptions { get; set; } = new();
    }
}
