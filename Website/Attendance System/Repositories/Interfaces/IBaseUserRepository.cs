using System.Threading.Tasks;
using Attendance_System.Models.BaseEntities;

namespace Attendance_System.Repositories.Interfaces
{
    public interface IBaseUserRepository : IGenericRepository<BaseUser>
    {
        Task<BaseUser?> GetByUsernameAsync(string username);
    }
}
