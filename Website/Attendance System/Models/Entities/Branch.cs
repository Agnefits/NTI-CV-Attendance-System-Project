using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class Branch : BaseModel
    {
        public required string Name { get; set; }
        public required string Location { get; set; }
    }
}
