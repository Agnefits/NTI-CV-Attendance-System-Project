using System;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class Token : BaseModel
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
        public string? Notes { get; set; }
        public DateTime? ExpireDate { get; set; }
        public Guid BaseUserId { get; set; }
        public virtual BaseUser? BaseUser { get; set; }
    }
}
