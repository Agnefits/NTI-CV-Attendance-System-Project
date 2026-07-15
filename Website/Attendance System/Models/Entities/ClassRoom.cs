using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class ClassRoom : BaseModel
    {
        public required string Title { get; set; }
        public required string Location { get; set; }
        public string? Notes { get; set; }
    }
}
