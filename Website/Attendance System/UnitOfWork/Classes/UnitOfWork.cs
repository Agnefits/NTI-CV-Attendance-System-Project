using System;
using System.Threading.Tasks;
using Attendance_System.Data;
using Attendance_System.Repositories.Classes;
using Attendance_System.Repositories.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;

namespace Attendance_System.UnitOfWork.Classes
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private bool _disposed = false;

        public IBaseUserRepository BaseUsers { get; }
        public IAdminUserRepository AdminUsers { get; }
        public IEmployeeRepository Employees { get; }
        public IStudentRepository Students { get; }
        public ITokenRepository Tokens { get; }
        public IBranchRepository Branches { get; }
        public ILevelRepository Levels { get; }
        public IClassRepository Classes { get; }
        public IClassRoomRepository ClassRooms { get; }
        public ILessonRepository Lessons { get; }
        public ICameraRepository Cameras { get; }
        public IStudentAttendanceRepository StudentAttendances { get; }
        public IEmployeeAttendanceRepository EmployeeAttendances { get; }
        public IFaceEmbeddingRepository FaceEmbeddings { get; }
        public ISettingRepository Settings { get; }
        public ILogRepository Logs { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            BaseUsers = new BaseUserRepository(_context);
            AdminUsers = new AdminUserRepository(_context);
            Employees = new EmployeeRepository(_context);
            Students = new StudentRepository(_context);
            Tokens = new TokenRepository(_context);
            Branches = new BranchRepository(_context);
            Levels = new LevelRepository(_context);
            Classes = new ClassRepository(_context);
            ClassRooms = new ClassRoomRepository(_context);
            Lessons = new LessonRepository(_context);
            Cameras = new CameraRepository(_context);
            StudentAttendances = new StudentAttendanceRepository(_context);
            EmployeeAttendances = new EmployeeAttendanceRepository(_context);
            FaceEmbeddings = new FaceEmbeddingRepository(_context);
            Settings = new SettingRepository(_context);
            Logs = new LogRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
