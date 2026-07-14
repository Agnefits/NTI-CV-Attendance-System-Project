using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Attendance_System.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName);
        void DeleteFile(string fileUrl);
    }
}
