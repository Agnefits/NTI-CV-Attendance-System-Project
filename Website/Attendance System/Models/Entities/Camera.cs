using System;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class Camera : BaseModel
    {
        public required string Title { get; set; }
        public string? Location { get; set; }
        public Guid? ClassRoomId { get; set; }
        public virtual ClassRoom? ClassRoom { get; set; }
        public required string Key { get; set; }
        public string? Notes { get; set; }
    }
}
