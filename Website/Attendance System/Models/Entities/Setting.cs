using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class Setting : BaseModel
    {
        public required string Key { get; set; }
        public required string Value { get; set; }
    }
}
