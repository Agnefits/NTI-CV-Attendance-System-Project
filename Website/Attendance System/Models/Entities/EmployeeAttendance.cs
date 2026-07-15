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

        /// <summary>The calendar date this attendance record belongs to.</summary>
        public DateTime AttendanceDate { get; set; } = DateTime.UtcNow.Date;

        /// <summary>
        /// Time the employee checked in (from camera or manual).
        /// Null if not yet checked in.
        /// </summary>
        public TimeSpan? CheckInTime { get; set; }

        /// <summary>
        /// Time the employee checked out (from camera or manual).
        /// Null if not yet checked out.
        /// </summary>
        public TimeSpan? CheckOutTime { get; set; }

        /// <summary>AI-reported confidence score for the face match (0.0 – 1.0).</summary>
        public float? RecognitionConfidence { get; set; }

        public string? Note { get; set; }
    }
}
