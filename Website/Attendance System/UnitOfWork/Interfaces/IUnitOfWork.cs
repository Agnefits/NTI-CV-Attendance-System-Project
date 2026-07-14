using System;
using System.Threading.Tasks;
using Attendance_System.Repositories.Interfaces;

namespace Attendance_System.UnitOfWork.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseUserRepository BaseUsers { get; }
        IAdminUserRepository AdminUsers { get; }
        IEmployeeRepository Employees { get; }
        IStudentRepository Students { get; }
        ITokenRepository Tokens { get; }
        IBranchRepository Branches { get; }
        ILevelRepository Levels { get; }
        IClassRepository Classes { get; }
        IClassRoomRepository ClassRooms { get; }
        ILessonRepository Lessons { get; }
        ICameraRepository Cameras { get; }
        IStudentAttendanceRepository StudentAttendances { get; }
        IEmployeeAttendanceRepository EmployeeAttendances { get; }
        IFaceEmbeddingRepository FaceEmbeddings { get; }
        ISettingRepository Settings { get; }
        ILogRepository Logs { get; }
        
        Task<int> SaveChangesAsync();
    }
}
