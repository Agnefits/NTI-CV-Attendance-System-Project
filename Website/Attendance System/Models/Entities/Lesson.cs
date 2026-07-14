using System;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Models.Entities
{
    public class Lesson : BaseModel
    {
        public Guid LevelId { get; set; }
        public virtual Level? Level { get; set; }
        
        public Guid? ClassId { get; set; }
        public virtual Class? Class { get; set; }
        
        public Guid TeacherId { get; set; }
        public virtual Employee? Teacher { get; set; }
        
        public Guid? ClassRoomId { get; set; }
        public virtual ClassRoom? ClassRoom { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DayOfWeek DayOfWeek { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
