using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.Services.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Attendance;

namespace Attendance_System.Controllers
{
    public class StudentAttendanceController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly IFaceAIService _faceAIService;

        public StudentAttendanceController(IUnitOfWork unitOfWork, IFileService fileService, IFaceAIService faceAIService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _faceAIService = faceAIService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(StudentAttendanceFilterViewModel filter)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var records = await _unitOfWork.StudentAttendances.GetAllAsync();
            var students = await _unitOfWork.Students.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();
            var lessons = await _unitOfWork.Lessons.GetAllAsync();

            if (userRole == Roles.Student.ToString() && Guid.TryParse(userIdStr, out Guid studentId))
            {
                records = records.Where(r => r.StudentId == studentId);
            }
            else
            {
                if (filter.Date.HasValue)
                {
                    records = records.Where(r => r.CreatedAt.Date == filter.Date.Value.Date);
                }
                if (filter.Status.HasValue)
                {
                    records = records.Where(r => r.Status == filter.Status.Value);
                }
                if (filter.ClassId.HasValue)
                {
                    var studentIdsInClass = students.Where(s => s.ClassId == filter.ClassId.Value).Select(s => s.Id);
                    records = records.Where(r => studentIdsInClass.Contains(r.StudentId));
                }
                if (filter.LevelId.HasValue)
                {
                    var studentIdsInLevel = students.Where(s => s.LevelId == filter.LevelId.Value).Select(s => s.Id);
                    records = records.Where(r => studentIdsInLevel.Contains(r.StudentId));
                }
                if (!string.IsNullOrEmpty(filter.SearchQuery))
                {
                    var matchedStudentIds = students
                        .Where(s => s.Fullname.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                    s.Username.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.Id);
                    records = records.Where(r => matchedStudentIds.Contains(r.StudentId));
                }
            }

            filter.Results = records.Select(r => {
                var s = students.FirstOrDefault(st => st.Id == r.StudentId);
                var l = lessons.FirstOrDefault(les => les.Id == r.LessonId);
                return new StudentAttendanceListViewModel
                {
                    Id = r.Id,
                    StudentId = r.StudentId,
                    StudentName = s?.Fullname ?? "Unknown",
                    StudentUsername = s?.Username ?? "Unknown",
                    ClassTitle = classes.FirstOrDefault(c => c.Id == s?.ClassId)?.Title ?? "None",
                    LevelTitle = levels.FirstOrDefault(lvl => lvl.Id == s?.LevelId)?.Title ?? "None",
                    Status = r.Status,
                    ByIA = r.ByIA,
                    CameraTitle = cameras.FirstOrDefault(c => c.Id == r.CameraId)?.Title,
                    LessonId = r.LessonId,
                    LessonTimeSlot = l != null ? $"{l.StartTime:hh\\:mm}-{l.EndTime:hh\\:mm} ({l.DayOfWeek})" : "General",
                    CreatedAt = r.CreatedAt,
                    Note = r.Note
                };
            }).OrderByDescending(r => r.CreatedAt).ToList();

            await PopulateFilterOptions(filter);

            ViewData["Title"] = "Student Attendance Ledger";
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var record = await _unitOfWork.StudentAttendances.GetByIdAsync(id);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            var students = await _unitOfWork.Students.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();
            var baseUsers = await _unitOfWork.BaseUsers.GetAllAsync();

            var s = students.FirstOrDefault(st => st.Id == record.StudentId);
            var creator = baseUsers.FirstOrDefault(u => u.Id == record.CreatedBy);
            var modifier = baseUsers.FirstOrDefault(u => u.Id == record.ModifiedBy);
            var cam = cameras.FirstOrDefault(c => c.Id == record.CameraId);

            var viewModel = new AttendanceDetailsViewModel
            {
                Id = record.Id,
                UserId = record.StudentId,
                UserFullname = s?.Fullname ?? "Unknown Student",
                UserType = "Student",
                UserEmail = s?.Email,
                UserImageUrl = s?.ImageUrl,
                Status = record.Status,
                ByIA = record.ByIA,
                CameraTitle = cam?.Title,
                CreatedByName = creator?.Username ?? "System",
                ModifiedByName = modifier?.Username ?? "System",
                CreatedAt = record.CreatedAt,
                LastModified = record.LastModified,
                Note = record.Note
            };

            ViewData["Title"] = "Student Attendance Details";
            return View(viewModel);
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public async Task<IActionResult> Create()
        {
            var model = new StudentAttendanceFormViewModel();
            await PopulateFormOptions(model);

            ViewData["Title"] = "Log Student Attendance";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public async Task<IActionResult> Create(StudentAttendanceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormOptions(model);
                return View(model);
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var record = new StudentAttendance
            {
                StudentId = model.StudentId,
                Status = model.Status,
                ByIA = false,
                CameraId = model.CameraId,
                LessonId = model.LessonId,
                Note = model.Note,
                CreatedBy = currentUserId,
                ModifiedBy = currentUserId
            };

            await _unitOfWork.StudentAttendances.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student attendance record logged successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var record = await _unitOfWork.StudentAttendances.GetByIdAsync(id);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new StudentAttendanceFormViewModel
            {
                Id = record.Id,
                StudentId = record.StudentId,
                Status = record.Status,
                ByIA = record.ByIA,
                CameraId = record.CameraId,
                LessonId = record.LessonId,
                Note = record.Note
            };

            await PopulateFormOptions(model);

            ViewData["Title"] = "Edit Student Attendance Log";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public async Task<IActionResult> Edit(StudentAttendanceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormOptions(model);
                return View(model);
            }

            var record = await _unitOfWork.StudentAttendances.GetByIdAsync(model.Id.GetValueOrDefault());
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            record.StudentId = model.StudentId;
            record.Status = model.Status;
            record.ByIA = model.ByIA;
            record.CameraId = model.CameraId;
            record.LessonId = model.LessonId;
            record.Note = model.Note;
            record.ModifiedBy = currentUserId;

            _unitOfWork.StudentAttendances.Update(record);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student attendance record updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var record = await _unitOfWork.StudentAttendances.GetByIdAsync(id);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.StudentAttendances.Delete(record);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student attendance record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public async Task<IActionResult> Report()
        {
            var records = await _unitOfWork.StudentAttendances.GetAllAsync();
            var total = records.Count();

            var viewModel = new AttendanceReportViewModel
            {
                TotalPresent = records.Count(r => r.Status == AttendanceStatus.Present),
                TotalAbsent = records.Count(r => r.Status == AttendanceStatus.Absent),
                TotalLate = records.Count(r => r.Status == AttendanceStatus.Late),
                TotalExcused = records.Count(r => r.Status == AttendanceStatus.Excused),
            };

            viewModel.PresentPercentage = total > 0 ? Math.Round((double)(viewModel.TotalPresent + viewModel.TotalLate) / total * 100, 1) : 100.0;

            var today = DateTime.Today;
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                viewModel.Dates.Add(date.ToString("dd MMM"));

                var daily = records.Where(r => r.CreatedAt.Date == date.Date);
                viewModel.DailyPresentCount.Add(daily.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late));
                viewModel.DailyAbsentCount.Add(daily.Count(r => r.Status == AttendanceStatus.Absent));
            }

            ViewData["Title"] = "Student Attendance Report";
            return View("~/Views/StudentAttendance/Report.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var records = await _unitOfWork.StudentAttendances.GetAllAsync();
            var students = await _unitOfWork.Students.GetAllAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("RecordID,StudentName,StudentUsername,Status,Detection,RecordedAt,Note");

            foreach (var r in records.OrderByDescending(x => x.CreatedAt))
            {
                var s = students.FirstOrDefault(st => st.Id == r.StudentId);
                var fullname = s?.Fullname ?? "Unknown";
                var username = s?.Username ?? "Unknown";
                var detection = r.ByIA ? "AI Camera" : "Manual Entry";
                csvBuilder.AppendLine($"{r.Id},{fullname},{username},{r.Status},{detection},{r.CreatedAt:yyyy-MM-dd HH:mm:ss},{r.Note}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(csvBytes, "text/csv", $"student_attendance_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public IActionResult UploadImage()
        {
            var model = new AIPictureUploadViewModel();
            ViewData["Title"] = "AI Student Check-in Scan";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
        public async Task<IActionResult> UploadImage(AIPictureUploadViewModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Please upload a valid image snapshot.");
                return View(model);
            }

            string base64Image = "";
            using (var ms = new System.IO.MemoryStream())
            {
                await model.File.CopyToAsync(ms);
                var bytes = ms.ToArray();
                base64Image = Convert.ToBase64String(bytes);
            }

            string imageUrl = await _fileService.UploadFileAsync(model.File, "attendance_scans");

            var students = await _unitOfWork.Students.GetAllAsync();
            var studentIds = students.Select(s => s.Id).ToHashSet();
            var embeddings = await _unitOfWork.FaceEmbeddings.FindAsync(e => studentIds.Contains(e.BaseUserId));
            var knownEmbeddings = embeddings.Select(e => new EmbeddingRecord
            {
                UserId        = e.BaseUserId,
                EmbeddingJson = e.EmbeddingJson
            }).ToList();

            if (!knownEmbeddings.Any())
            {
                model.IsProcessed = true;
                model.IsSuccess = false;
                model.Message = "No student face embeddings registered in the system.";
                return View(model);
            }

            // Run face recognition using real AI service
            var matches = await _faceAIService.RecognizeFacesAsync(base64Image, knownEmbeddings);

            // Retrieve security/confidence threshold from settings
            var thresholdSettings = await _unitOfWork.Settings.FindAsync(s => s.Key == "SecurityThreshold");
            float threshold = 0.40f;
            if (thresholdSettings.FirstOrDefault() is { } ts && float.TryParse(ts.Value, out var t))
                threshold = t;

            var confidentMatches = matches?.Where(m => m.Confidence >= threshold).ToList() ?? new List<FaceMatch>();

            if (!confidentMatches.Any())
            {
                model.IsProcessed = true;
                model.IsSuccess = false;
                model.UploadedImageUrl = imageUrl;
                model.Message = "No recognized student faces matching the uploaded image (or confidence score is too low).";
                ViewData["Title"] = "AI Student Check-in Scan";
                return View(model);
            }

            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? loggedBy = Guid.TryParse(currentUserIdStr, out Guid logId) ? logId : null;

            var now = DateTime.Now;
            var nowTime = now.TimeOfDay;
            var nowDay = now.DayOfWeek;

            int recorded = 0;
            foreach (var match in confidentMatches)
            {
                var matchedStudent = students.FirstOrDefault(s => s.Id == match.UserId);
                if (matchedStudent is null) continue;

                var lessons = await _unitOfWork.Lessons.FindAsync(l =>
                    l.ClassId == matchedStudent.ClassId &&
                    l.DayOfWeek == nowDay &&
                    l.StartTime <= nowTime &&
                    l.EndTime >= nowTime &&
                    (l.StartDate == null || l.StartDate <= now) &&
                    (l.EndDate == null || l.EndDate >= now));

                var activeLesson = lessons.FirstOrDefault();

                // Guard: check if already marked present for this lesson today
                var existing = await _unitOfWork.StudentAttendances.FindAsync(a =>
                    a.StudentId == matchedStudent.Id &&
                    a.LessonId == (activeLesson != null ? activeLesson.Id : null) &&
                    a.CreatedAt.Date == now.Date);

                if (!existing.Any())
                {
                    var record = new StudentAttendance
                    {
                        StudentId = matchedStudent.Id,
                        Status = AttendanceStatus.Present,
                        ByIA = true,
                        LessonId = activeLesson?.Id,
                        Note = "Detected via AI face snapshot upload scan.",
                        CreatedBy = loggedBy,
                        ModifiedBy = loggedBy,
                        RecognitionConfidence = match.Confidence
                    };

                    await _unitOfWork.StudentAttendances.AddAsync(record);
                    recorded++;
                }

                model.Matches.Add(new MatchedCandidateViewModel
                {
                    Name = matchedStudent.Fullname,
                    Confidence = Math.Round(match.Confidence * 100.0, 1),
                    BoundingBox = match.BoundingBox,
                    Role = "Student"
                });
            }

            if (recorded > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            if (!model.Matches.Any())
            {
                model.IsProcessed = true;
                model.IsSuccess = false;
                model.UploadedImageUrl = imageUrl;
                model.Message = "No registered students identified in this image.";
                ViewData["Title"] = "AI Student Check-in Scan";
                return View(model);
            }

            // Set backward compatible fields to the best match
            var bestMatch = model.Matches.OrderByDescending(m => m.Confidence).First();
            model.MatchedName = bestMatch.Name;
            model.Confidence = bestMatch.Confidence;
            model.BoundingBox = bestMatch.BoundingBox;

            model.IsProcessed = true;
            model.IsSuccess = true;
            model.UploadedImageUrl = imageUrl;
            model.Message = $"Recognized {model.Matches.Count} face(s) successfully. Attendance marked for {recorded} student(s).";

            ViewData["Title"] = "AI Student Check-in Scan";
            return View(model);
        }

        private async Task PopulateFilterOptions(StudentAttendanceFilterViewModel model)
        {
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var levels = await _unitOfWork.Levels.GetAllAsync();

            model.ClassOptions = classes.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToList();
            model.LevelOptions = levels.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Title }).ToList();
            model.StatusOptions = Enum.GetValues(typeof(AttendanceStatus)).Cast<AttendanceStatus>().Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.ToString()
            }).ToList();
        }

        private async Task PopulateFormOptions(StudentAttendanceFormViewModel model)
        {
            var students = await _unitOfWork.Students.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();
            var lessons = await _unitOfWork.Lessons.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();

            model.StudentOptions = students.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Fullname} ({s.Username})"
            }).ToList();

            model.CameraOptions = cameras.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Title
            }).ToList();

            model.LessonOptions = lessons.Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = $"{classes.FirstOrDefault(c => c.Id == l.ClassId)?.Title ?? "Class"} - {l.StartTime:hh\\:mm}-{l.EndTime:hh\\:mm} ({l.DayOfWeek})"
            }).ToList();

            model.StatusOptions = Enum.GetValues(typeof(AttendanceStatus)).Cast<AttendanceStatus>().Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.ToString()
            }).ToList();
        }
    }
}
