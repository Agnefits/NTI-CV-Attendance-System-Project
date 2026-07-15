using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Attendance_System.Data;
using Attendance_System.Models.BaseEntities;
using Attendance_System.Repositories.Interfaces;

namespace Attendance_System.Repositories.Classes
{
    public class BaseUserRepository : GenericRepository<BaseUser>, IBaseUserRepository
    {
        public BaseUserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<BaseUser?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
