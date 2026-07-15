using System;
using Attendance_System.Models.BaseEntities;
using Attendance_System.Models.Enums;

namespace Attendance_System.Models.Entities
{
    public class Employee : BaseUser
    {
        public Employee()
        {
            Role = Roles.Employee;
        }

        public required string Fullname { get; set; }
        public required string JobTitle { get; set; }
        public required string Speciality { get; set; }
        public Guid? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}
