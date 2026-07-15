using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.Services.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;

namespace Attendance_System.Controllers
{
    /// <summary>
    /// Two responsibilities:
    ///  1. POST /api/attendance/scan  — Admin submits a camera frame (base64 image).
    ///     Controller resolves the active lesson for that camera's classroom, fetches
    ///     scoped embeddings, calls the AI service, then records attendance.
    ///
    ///  2. POST /api/attendance/employee-scan — Same flow but for employees:
    ///     uses employee check-in/out windows from Settings to decide if it is a
    ///     check-in or check-out event.
    ///
    /// Secured with X-AI-Secret-Key header (same key used by the camera watcher script).
    /// </summary>
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceWebhookController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaceAIService _faceAIService;
        private readonly string _secretKey;

        public AttendanceWebhookController(
            IUnitOfWork unitOfWork,
            IFaceAIService faceAIService,
            IConfiguration configuration)
        {
            _unitOfWork    = unitOfWork;
            _faceAIService = faceAIService;
            _secretKey     = configuration["FaceAISettings:SecretKey"] ?? "change-me";
        }

        // ─── Shared key validation ────────────────────────────────────────────────
        private bool IsAuthorized() =>
            Request.Headers.TryGetValue("X-AI-Secret-Key", out var key) && key == _secretKey;

        // ─── POST /api/attendance/scan ────────────────────────────────────────────
        /// <summary>
        /// Receive a frame from a camera, identify the active lesson in that room,
        /// fetch only the embeddings for students in that lesson's class, run recognition,
        /// and create StudentAttendance records.
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> ScanForStudents([FromBody] ScanRequest request)
        {
            if (!IsAuthorized())
                return Unauthorized(new { message = "Invalid or missing X-AI-Secret-Key." });

            if (string.IsNullOrWhiteSpace(request.ImageBase64))
                return BadRequest(new { message = "ImageBase64 is required." });

            Camera camera = null;
            if (!string.IsNullOrWhiteSpace(request.CameraKey))
            {
                var cameras = await _unitOfWork.Cameras.FindAsync(c => c.Key == request.CameraKey);
                camera = cameras.FirstOrDefault();
            }
            else if (request.CameraId != Guid.Empty)
            {
                camera = await _unitOfWork.Cameras.GetByIdAsync(request.CameraId);
            }

            if (camera is null)
                return NotFound(new { message = "Camera not found." });

            if (camera.ClassRoomId is null)
                return BadRequest(new { message = "Camera is not assigned to a classroom." });

            // 2. Find the active lesson for this classroom at the current time
            var now     = DateTime.Now;
            var nowTime = now.TimeOfDay;
            var nowDay  = now.DayOfWeek;

            var lessons = await _unitOfWork.Lessons.FindAsync(l =>
                l.ClassRoomId == camera.ClassRoomId &&
                l.DayOfWeek   == nowDay &&
                l.StartTime   <= nowTime &&
                l.EndTime     >= nowTime &&
                (l.StartDate == null || l.StartDate <= now) &&
                (l.EndDate   == null || l.EndDate   >= now));

            var activeLesson = lessons.FirstOrDefault();
            if (activeLesson is null)
                return Ok(new { message = "No active lesson found for this classroom at the current time.", recorded = 0 });

            // 3. Get all students enrolled in the lesson's class
            var students = await _unitOfWork.Students.FindAsync(s => s.ClassId == activeLesson.ClassId);
            var studentIds = students.Select(s => s.Id).ToHashSet();

            // 4. Fetch embeddings scoped to those students only
            var embeddings = await _unitOfWork.FaceEmbeddings.FindAsync(e => studentIds.Contains(e.BaseUserId));
            var knownEmbeddings = embeddings.Select(e => new EmbeddingRecord
            {
                UserId        = e.BaseUserId,
                EmbeddingJson = e.EmbeddingJson
            }).ToList();

            if (!knownEmbeddings.Any())
                return Ok(new { message = "No enrolled faces found for students in this class.", recorded = 0 });

            // 5. Call AI service with scoped embeddings
            var matches = await _faceAIService.RecognizeFacesAsync(request.ImageBase64, knownEmbeddings);

            // 6. Load confidence threshold from settings
            var thresholdSettings = await _unitOfWork.Settings.FindAsync(s => s.Key == "SecurityThreshold");
            float threshold = 0.40f;
            if (thresholdSettings.FirstOrDefault() is { } ts && float.TryParse(ts.Value, out var t))
                threshold = t;

            // 7. Record attendance for each confident match
            int recorded = 0;
            foreach (var match in matches.Where(m => m.Confidence >= threshold))
            {
                // Guard: skip if already marked present for this lesson today
                var existing = await _unitOfWork.StudentAttendances.FindAsync(a =>
                    a.StudentId == match.UserId &&
                    a.LessonId  == activeLesson.Id &&
                    a.CreatedAt.Date == now.Date);

                if (existing.Any()) continue;

                var attendance = new StudentAttendance
                {
                    StudentId              = match.UserId,
                    LessonId               = activeLesson.Id,
                    CameraId               = camera.Id,
                    Status                 = AttendanceStatus.Present,
                    ByIA                   = true,
                    RecognitionConfidence  = match.Confidence
                };

                await _unitOfWork.StudentAttendances.AddAsync(attendance);
                recorded++;
            }

            if (recorded > 0)
                await _unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                lessonId    = activeLesson.Id,
                classId     = activeLesson.ClassId,
                totalFaces  = matches.Count,
                recorded,
                message     = $"Recorded attendance for {recorded} student(s)."
            });
        }

        // ─── POST /api/attendance/employee-scan ───────────────────────────────────
        /// <summary>
        /// Receive a frame from an employee check-in/out camera.
        /// Uses EmployeeCheckIn/Out time windows from Settings to decide event type.
        /// Creates or updates EmployeeAttendance (CheckInTime / CheckOutTime).
        /// </summary>
        [HttpPost("employee-scan")]
        public async Task<IActionResult> ScanForEmployees([FromBody] ScanRequest request)
        {
            if (!IsAuthorized())
                return Unauthorized(new { message = "Invalid or missing X-AI-Secret-Key." });

            if (string.IsNullOrWhiteSpace(request.ImageBase64))
                return BadRequest(new { message = "ImageBase64 is required." });

            Camera camera = null;
            if (!string.IsNullOrWhiteSpace(request.CameraKey))
            {
                var cameras = await _unitOfWork.Cameras.FindAsync(c => c.Key == request.CameraKey);
                camera = cameras.FirstOrDefault();
            }
            else if (request.CameraId != Guid.Empty)
            {
                camera = await _unitOfWork.Cameras.GetByIdAsync(request.CameraId);
            }

            if (camera is null)
                return NotFound(new { message = "Camera not found." });

            var now     = DateTime.Now;
            var nowTime = now.TimeOfDay;
            var today   = now.Date;

            // Load time-window settings
            var allSettings = await _unitOfWork.Settings.GetAllAsync();
            TimeSpan ParseTime(string key, string fallback) =>
                TimeSpan.TryParse(allSettings.FirstOrDefault(s => s.Key == key)?.Value ?? fallback, out var ts) ? ts : TimeSpan.Parse(fallback);

            var checkInStart  = ParseTime("EmployeeCheckInStart",  "07:30");
            var checkInEnd    = ParseTime("EmployeeCheckInEnd",    "09:30");
            var checkOutStart = ParseTime("EmployeeCheckOutStart", "15:00");
            var checkOutEnd   = ParseTime("EmployeeCheckOutEnd",   "19:00");

            bool isCheckIn  = nowTime >= checkInStart  && nowTime <= checkInEnd;
            bool isCheckOut = nowTime >= checkOutStart && nowTime <= checkOutEnd;

            if (!isCheckIn && !isCheckOut)
                return Ok(new { message = "Outside check-in and check-out time windows. No attendance recorded.", recorded = 0 });

            // Load confidence threshold
            var thresholdSettings = await _unitOfWork.Settings.FindAsync(s => s.Key == "SecurityThreshold");
            float threshold = 0.40f;
            if (thresholdSettings.FirstOrDefault() is { } ts2 && float.TryParse(ts2.Value, out var tv))
                threshold = tv;

            // Fetch all employee embeddings
            var employees = await _unitOfWork.Employees.GetAllAsync();
            var employeeIds = employees.Select(e => e.Id).ToHashSet();
            var embeddings  = await _unitOfWork.FaceEmbeddings.FindAsync(e => employeeIds.Contains(e.BaseUserId));

            var knownEmbeddings = embeddings.Select(e => new EmbeddingRecord
            {
                UserId        = e.BaseUserId,
                EmbeddingJson = e.EmbeddingJson
            }).ToList();

            if (!knownEmbeddings.Any())
                return Ok(new { message = "No enrolled employee faces found.", recorded = 0 });

            // Run face recognition
            var matches = await _faceAIService.RecognizeFacesAsync(request.ImageBase64, knownEmbeddings);

            int recorded = 0;
            foreach (var match in matches.Where(m => m.Confidence >= threshold))
            {
                // Find or create today's record for this employee
                var existing = (await _unitOfWork.EmployeeAttendances.FindAsync(a =>
                    a.EmployeeId == match.UserId &&
                    a.AttendanceDate == today)).FirstOrDefault();

                if (isCheckIn)
                {
                    if (existing is null)
                    {
                        // New check-in record
                        var record = new EmployeeAttendance
                        {
                            EmployeeId             = match.UserId,
                            CameraId               = camera.Id,
                            AttendanceDate         = today,
                            CheckInTime            = nowTime,
                            Status                 = AttendanceStatus.Present,
                            ByIA                   = true,
                            RecognitionConfidence  = match.Confidence
                        };
                        await _unitOfWork.EmployeeAttendances.AddAsync(record);
                        recorded++;
                    }
                    // Already checked in → skip
                }
                else if (isCheckOut && existing is not null && existing.CheckOutTime is null)
                {
                    // Update existing record with check-out time
                    existing.CheckOutTime           = nowTime;
                    existing.RecognitionConfidence  = match.Confidence;
                    _unitOfWork.EmployeeAttendances.Update(existing);
                    recorded++;
                }
            }

            if (recorded > 0)
                await _unitOfWork.SaveChangesAsync();

            var eventType = isCheckIn ? "check-in" : "check-out";
            return Ok(new
            {
                eventType,
                totalFaces = matches.Count,
                recorded,
                message = $"Recorded {eventType} for {recorded} employee(s)."
            });
        }

        // ─── GET /api/attendance/active-embeddings ────────────────────────────────
        /// <summary>
        /// Retrieve all registered face embeddings in the system, along with the user's
        /// name and ID, to sync local camera clients.
        /// </summary>
        [HttpGet("active-embeddings")]
        public async Task<IActionResult> GetActiveEmbeddings()
        {
            if (!IsAuthorized())
                return Unauthorized(new { message = "Invalid or missing X-AI-Secret-Key." });

            var embeddings = await _unitOfWork.FaceEmbeddings.GetAllAsync();
            
            var students  = await _unitOfWork.Students.GetAllAsync();
            var employees = await _unitOfWork.Employees.GetAllAsync();
            var admins    = await _unitOfWork.AdminUsers.GetAllAsync();

            var studentDict  = students.ToDictionary(s => s.Id, s => s.Fullname);
            var employeeDict = employees.ToDictionary(e => e.Id, e => e.Fullname);
            var adminDict    = admins.ToDictionary(a => a.Id, a => a.Fullname);

            var result = embeddings.Select(e =>
            {
                string name = "Unknown User";
                if (studentDict.TryGetValue(e.BaseUserId, out var sn))
                    name = sn;
                else if (employeeDict.TryGetValue(e.BaseUserId, out var en))
                    name = en;
                else if (adminDict.TryGetValue(e.BaseUserId, out var an))
                    name = an;

                return new
                {
                    user_id = e.BaseUserId.ToString(),
                    user_name = name,
                    embedding_json = e.EmbeddingJson
                };
            }).ToList();

            return Ok(result);
        }

        // ─── GET /api/attendance/employee-windows ────────────────────────────────
        /// <summary>
        /// Retrieve the employee check-in and check-out time windows from settings.
        /// </summary>
        [HttpGet("employee-windows")]
        public async Task<IActionResult> GetEmployeeWindows()
        {
            if (!IsAuthorized())
                return Unauthorized(new { message = "Invalid or missing X-AI-Secret-Key." });

            var allSettings = await _unitOfWork.Settings.GetAllAsync();

            var checkInStart  = allSettings.FirstOrDefault(s => s.Key == "EmployeeCheckInStart")?.Value ?? "07:30";
            var checkInEnd    = allSettings.FirstOrDefault(s => s.Key == "EmployeeCheckInEnd")?.Value ?? "09:30";
            var checkOutStart = allSettings.FirstOrDefault(s => s.Key == "EmployeeCheckOutStart")?.Value ?? "15:00";
            var checkOutEnd   = allSettings.FirstOrDefault(s => s.Key == "EmployeeCheckOutEnd")?.Value ?? "19:00";

            return Ok(new
            {
                checkInStart,
                checkInEnd,
                checkOutStart,
                checkOutEnd
            });
        }
    }

    // ─── Shared request DTO ───────────────────────────────────────────────────────
    public class ScanRequest
    {
        /// <summary>Base64-encoded camera frame (JPEG/PNG).</summary>
        public string ImageBase64 { get; set; } = string.Empty;

        /// <summary>The camera ID that captured this frame.</summary>
        public Guid CameraId { get; set; }

        /// <summary>The camera Key that captured this frame.</summary>
        public string CameraKey { get; set; } = string.Empty;
    }
}
