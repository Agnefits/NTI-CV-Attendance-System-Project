using System;
using System.ComponentModel.DataAnnotations;

namespace Attendance_System.ViewModels.Level
{
    public class LevelFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Level Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;
    }
}
