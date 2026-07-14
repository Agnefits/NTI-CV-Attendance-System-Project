using Attendance_System.Data;
using Attendance_System.Models.Entities;
using Attendance_System.Repositories.Interfaces;

namespace Attendance_System.Repositories.Classes
{
    public class FaceEmbeddingRepository : GenericRepository<FaceEmbedding>, IFaceEmbeddingRepository
    {
        public FaceEmbeddingRepository(AppDbContext context) : base(context)
        {
        }
    }
}
