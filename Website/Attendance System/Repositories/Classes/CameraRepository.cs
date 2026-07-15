using Attendance_System.Data;
using Attendance_System.Models.Entities;
using Attendance_System.Repositories.Interfaces;

namespace Attendance_System.Repositories.Classes
{
    public class CameraRepository : GenericRepository<Camera>, ICameraRepository
    {
        public CameraRepository(AppDbContext context) : base(context)
        {
        }
    }
}
