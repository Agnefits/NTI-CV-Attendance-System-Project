using System;
using Attendance_System.Models.BaseEntities;
using Attendance_System.Models.Enums;

namespace Attendance_System.Models.Entities
{
    public class EmployeeAttendance : BaseModel
    {
        public AttendanceStatus Status { get; set; }
        public bool ByIA { get; set; }
        
        public Guid? CameraId { get; set; }
        public virtual Camera? Camera { get; set; }
        
        public Guid? CreatedBy { get; set; }
        public virtual BaseUser? CreatedByUser { get; set; }
        
        public Guid? ModifiedBy { get; set; }
        public virtual BaseUser? ModifiedByUser { get; set; }
        
        public Guid EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }
        
        public string? Note { get; set; }
    }
}
