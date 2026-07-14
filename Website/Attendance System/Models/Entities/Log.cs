using System;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class Log : BaseModel
    {
        public Guid ByUser { get; set; }
        public virtual BaseUser? User { get; set; }
        public required string Action { get; set; }
        public required string ItemTable { get; set; }
        public Guid ItemId { get; set; }
        public string? Description { get; set; }
    }
}
