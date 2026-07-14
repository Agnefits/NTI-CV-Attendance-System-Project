using System;
using Attendance_System.Models.BaseEntities;
using Attendance_System.Models.Enums;

namespace Attendance_System.Models.Entities
{
    public class Student : BaseUser
    {
        public Student()
        {
            Role = Roles.Student;
        }

        public required string Fullname { get; set; }
        public Guid LevelId { get; set; }
        public virtual Level? Level { get; set; }
        public Guid? ClassId { get; set; }
        public virtual Class? Class { get; set; }
    }
}
