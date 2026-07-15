using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.Services.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;

namespace Attendance_System.Controllers
{
    /// <summary>
    /// Handles face enrollment (extract + store embeddings) for any user type —
    /// Student, Employee, Teacher, or Admin.
    /// All endpoints return JSON so the admin UI can call them from JavaScript
    /// (webcam capture or file upload).
    /// </summary>
    [AuthorizedRoles(Roles.Admin)]
    public class FaceEnrollmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaceAIService _faceAIService;

        public FaceEnrollmentController(IUnitOfWork unitOfWork, IFaceAIService faceAIService)
        {
            _unitOfWork  = unitOfWork;
            _faceAIService = faceAIService;
        }

        // ─── GET /FaceEnrollment/Preview/{id} ────────────────────────────────
        // Returns a JSON list of stored embeddings for a user (for the admin UI).
        [HttpGet("FaceEnrollment/Preview/{id}")]
        public async Task<IActionResult> Preview(Guid id)
        {
            var embeddings = await _unitOfWork.FaceEmbeddings.FindAsync(e => e.BaseUserId == id);
            var result = embeddings.Select(e => new
            {
                e.Id,
                e.QualityScore,
                e.CaptureAngle,
                e.Label,
                e.CreatedAt
            });
            return Json(new { success = true, data = result });
        }

        // ─── POST /FaceEnrollment/Enroll ─────────────────────────────────────────
        // Body: { userId: Guid, imageBase64: string, captureAngle?: string, label?: string }
        [HttpPost("FaceEnrollment/Enroll")]
        public async Task<IActionResult> Enroll([FromBody] EnrollRequest model)
        {
            if (model.UserId == Guid.Empty || string.IsNullOrWhiteSpace(model.ImageBase64))
                return Json(new { success = false, message = "UserId and ImageBase64 are required." });

            // Verify user exists
            var user = await _unitOfWork.BaseUsers.GetByIdAsync(model.UserId);
            if (user is null)
                return Json(new { success = false, message = "User not found." });

            // Call AI microservice to extract embedding
            var result = await _faceAIService.ExtractEmbeddingAsync(model.ImageBase64);
            if (result is null)
                return Json(new { success = false, message = "No face detected or quality too low. Please retake the photo." });

            // Retrieve min quality threshold from settings
            var qualitySettings = await _unitOfWork.Settings.FindAsync(s => s.Key == "MinEmbeddingQuality");
            float minQuality = 0.60f;
            if (qualitySettings.FirstOrDefault() is { } qs && float.TryParse(qs.Value, out var q))
                minQuality = q;

            if (result.QualityScore < minQuality)
                return Json(new
                {
                    success = false,
                    message = $"Image quality too low ({result.QualityScore:P0}). Minimum required: {minQuality:P0}. Please use better lighting or a clearer photo."
                });

            // Store embedding
            var embedding = new FaceEmbedding
            {
                BaseUserId    = model.UserId,
                EmbeddingJson = result.EmbeddingJson,
                QualityScore  = result.QualityScore,
                CaptureAngle  = model.CaptureAngle?.Trim(),
                Label         = model.Label?.Trim()
            };

            await _unitOfWork.FaceEmbeddings.AddAsync(embedding);
            await _unitOfWork.SaveChangesAsync();

            return Json(new
            {
                success = true,
                embeddingId   = embedding.Id,
                qualityScore  = embedding.QualityScore,
                message = "Face enrolled successfully."
            });
        }

        // ─── DELETE /FaceEnrollment/Delete/{id} ─────────────────────────
        [HttpPost("FaceEnrollment/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var embedding = await _unitOfWork.FaceEmbeddings.GetByIdAsync(id);
            if (embedding is null)
                return Json(new { success = false, message = "Embedding not found." });

            _unitOfWork.FaceEmbeddings.Delete(embedding);
            await _unitOfWork.SaveChangesAsync();

            return Json(new { success = true, message = "Embedding deleted." });
        }

        // ─── POST /FaceEnrollment/RebuildIndex ───────────────────────────────────
        // Pushes all stored embeddings to the AI service's FAISS index.
        // Call this after bulk enrollment or if the AI service is restarted.
        [HttpPost("FaceEnrollment/RebuildIndex")]
        public async Task<IActionResult> RebuildIndex()
        {
            var all = await _unitOfWork.FaceEmbeddings.GetAllAsync();
            var records = all.Select(e => new EmbeddingRecord
            {
                UserId        = e.BaseUserId,
                EmbeddingJson = e.EmbeddingJson
            }).ToList();

            await _faceAIService.RebuildIndexAsync(records);

            return Json(new { success = true, count = records.Count, message = "FAISS index rebuilt successfully." });
        }

        // ─── GET /FaceEnrollment/ServiceHealth ───────────────────────────────────
        [HttpGet("FaceEnrollment/ServiceHealth")]
        public async Task<IActionResult> ServiceHealth()
        {
            var healthy = await _faceAIService.IsHealthyAsync();
            return Json(new { healthy, message = healthy ? "AI service is online." : "AI service is unreachable." });
        }
    }

    // ─── Request DTO ─────────────────────────────────────────────────────────────
    public class EnrollRequest
    {
        public Guid UserId { get; set; }

        /// <summary>Base64-encoded JPEG/PNG from webcam or file picker.</summary>
        public string ImageBase64 { get; set; } = string.Empty;

        /// <summary>Optional: "front", "slight-left", "slight-right".</summary>
        public string? CaptureAngle { get; set; }

        /// <summary>Optional: human label for this sample, e.g. "Enrollment shot 1".</summary>
        public string? Label { get; set; }
    }
}
