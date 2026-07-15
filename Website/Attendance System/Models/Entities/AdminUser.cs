using System;
using Attendance_System.Models.BaseEntities;
using Attendance_System.Models.Enums;

namespace Attendance_System.Models.Entities
{
    public class AdminUser : BaseUser
    {
        public AdminUser()
        {
            Role = Roles.Admin;
        }

        public required string Fullname { get; set; }
        public Guid? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}
