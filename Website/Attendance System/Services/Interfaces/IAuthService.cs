using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Services.Interfaces
{
    public interface IAuthService
    {
        string GenerateToken(BaseUser user);
    }
}
