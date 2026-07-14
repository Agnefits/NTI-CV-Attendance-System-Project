using System;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class Class : BaseModel
    {
        public required string Title { get; set; }
        public Guid LevelId { get; set; }
        public virtual Level? Level { get; set; }
    }
}
