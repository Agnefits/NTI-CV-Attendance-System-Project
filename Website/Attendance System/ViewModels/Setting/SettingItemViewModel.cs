using System;
using System.ComponentModel.DataAnnotations;

namespace Attendance_System.ViewModels.Setting
{
    public class SettingItemViewModel
    {
        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Group { get; set; }
    }
}
