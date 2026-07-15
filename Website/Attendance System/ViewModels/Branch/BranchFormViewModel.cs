using System;
using System.ComponentModel.DataAnnotations;

namespace Attendance_System.ViewModels.Branch
{
    public class BranchFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Branch Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; } = string.Empty;
    }
}
