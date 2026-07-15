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
        public double[]? BoundingBox { get; set; }

        public System.Collections.Generic.List<MatchedCandidateViewModel> Matches { get; set; } = new();
    }

    public class MatchedCandidateViewModel
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public double[]? BoundingBox { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
