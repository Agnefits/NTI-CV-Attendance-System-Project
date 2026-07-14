using System;
using System.ComponentModel.DataAnnotations;

namespace Attendance_System.ViewModels.ClassRoom
{
    public class ClassRoomFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Classroom Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }
}
