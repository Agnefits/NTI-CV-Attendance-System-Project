using Attendance_System.Models.Enums;

namespace Attendance_System.Models.BaseEntities
{
    public abstract class BaseUser : BaseModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ImageUrl { get; set; }
        public Roles Role { get; set; }
    }
}
