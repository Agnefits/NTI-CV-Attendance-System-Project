using Microsoft.AspNetCore.Http;

namespace Attendance_System.ViewModels.Attendance
{
    public class AIPictureUploadViewModel
    {
        public IFormFile? File { get; set; }
        public bool IsProcessed { get; set; } = false;
        public bool IsSuccess { get; set; } = false;
        public string MatchedName { get; set; } = string.Empty;
        public double Confidence { get; set; } = 0.0;
        public string UploadedImageUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
