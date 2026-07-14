using Attendance_System.Data;
using Attendance_System.Models.Entities;
using Attendance_System.Repositories.Interfaces;

namespace Attendance_System.Repositories.Classes
{
    public class LevelRepository : GenericRepository<Level>, ILevelRepository
    {
        public LevelRepository(AppDbContext context) : base(context)
        {
        }
    }
}
