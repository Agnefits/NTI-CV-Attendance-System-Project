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
    [AuthorizedRoles(Roles.Admin, Roles.Employee, Roles.Teacher)]
    public class EmployeeAttendanceController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly IFaceAIService _faceAIService;

        public EmployeeAttendanceController(IUnitOfWork unitOfWork, IFileService fileService, IFaceAIService faceAIService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _faceAIService = faceAIService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(EmployeeAttendanceFilterViewModel filter)
        {
            var records = await _unitOfWork.EmployeeAttendances.GetAllAsync();
            var employees = await _unitOfWork.Employees.GetAllAsync();
            var branches = await _unitOfWork.Branches.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if ((userRole == Roles.Employee.ToString() || userRole == Roles.Teacher.ToString()) && Guid.TryParse(userIdStr, out Guid employeeId))
            {
                records = records.Where(r => r.EmployeeId == employeeId);
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
                if (filter.BranchId.HasValue)
                {
                    var employeeIdsInBranch = employees.Where(e => e.BranchId == filter.BranchId.Value).Select(e => e.Id);
                    records = records.Where(r => employeeIdsInBranch.Contains(r.EmployeeId));
                }
                if (!string.IsNullOrEmpty(filter.SearchQuery))
                {
                    var matchedEmployeeIds = employees
                        .Where(e => e.Fullname.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                    e.Username.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase))
                        .Select(e => e.Id);
                    records = records.Where(r => matchedEmployeeIds.Contains(r.EmployeeId));
                }
            }

            filter.Results = records.Select(r => {
                var emp = employees.FirstOrDefault(e => e.Id == r.EmployeeId);
                return new EmployeeAttendanceListViewModel
                {
                    Id = r.Id,
                    EmployeeId = r.EmployeeId,
                    EmployeeName = emp?.Fullname ?? "Unknown",
                    EmployeeUsername = emp?.Username ?? "Unknown",
                    JobTitle = emp?.JobTitle ?? "None",
                    BranchName = branches.FirstOrDefault(b => b.Id == emp?.BranchId)?.Name ?? "General",
                    Status = r.Status,
                    ByIA = r.ByIA,
                    CameraTitle = cameras.FirstOrDefault(c => c.Id == r.CameraId)?.Title,
                    CreatedAt = r.CreatedAt,
                    Note = r.Note
                };
            }).OrderByDescending(r => r.CreatedAt).ToList();

            await PopulateFilterOptions(filter);

            ViewData["Title"] = "Employee Attendance Ledger";
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var record = await _unitOfWork.EmployeeAttendances.GetByIdAsync(id);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != Roles.Admin.ToString() && record.EmployeeId.ToString() != userIdStr)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this record.";
                return RedirectToAction(nameof(Index));
            }

            var employees = await _unitOfWork.Employees.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();
            var baseUsers = await _unitOfWork.BaseUsers.GetAllAsync();

            var emp = employees.FirstOrDefault(e => e.Id == record.EmployeeId);
            var creator = baseUsers.FirstOrDefault(u => u.Id == record.CreatedBy);
            var modifier = baseUsers.FirstOrDefault(u => u.Id == record.ModifiedBy);
            var cam = cameras.FirstOrDefault(c => c.Id == record.CameraId);

            var viewModel = new AttendanceDetailsViewModel
            {
                Id = record.Id,
                UserId = record.EmployeeId,
                UserFullname = emp?.Fullname ?? "Unknown Employee",
                UserType = "Employee",
                UserEmail = emp?.Email,
                UserImageUrl = emp?.ImageUrl,
                Status = record.Status,
                ByIA = record.ByIA,
                CameraTitle = cam?.Title,
                CreatedByName = creator?.Username ?? "System",
                ModifiedByName = modifier?.Username ?? "System",
                CreatedAt = record.CreatedAt,
                LastModified = record.LastModified,
                Note = record.Note
            };

            ViewData["Title"] = "Employee Attendance Details";
            return View(viewModel);
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Create()
        {
            var model = new EmployeeAttendanceFormViewModel();
            await PopulateFormOptions(model);

            ViewData["Title"] = "Log Employee Attendance";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Create(EmployeeAttendanceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormOptions(model);
                return View(model);
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var record = new EmployeeAttendance
            {
                EmployeeId = model.EmployeeId,
                Status = model.Status,
                ByIA = false,
                CameraId = model.CameraId,
                Note = model.Note,
                CreatedBy = currentUserId,
                ModifiedBy = currentUserId
            };

            await _unitOfWork.EmployeeAttendances.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee attendance record logged successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var record = await _unitOfWork.EmployeeAttendances.GetByIdAsync(id);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new EmployeeAttendanceFormViewModel
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                Status = record.Status,
                ByIA = record.ByIA,
                CameraId = record.CameraId,
                Note = record.Note
            };

            await PopulateFormOptions(model);

            ViewData["Title"] = "Edit Employee Attendance Log";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Edit(EmployeeAttendanceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormOptions(model);
                return View(model);
            }

            var record = await _unitOfWork.EmployeeAttendances.GetByIdAsync(model.Id.GetValueOrDefault());
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            record.EmployeeId = model.EmployeeId;
            record.Status = model.Status;
            record.ByIA = model.ByIA;
            record.CameraId = model.CameraId;
            record.Note = model.Note;
            record.ModifiedBy = currentUserId;

            _unitOfWork.EmployeeAttendances.Update(record);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee attendance record updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var record = await _unitOfWork.EmployeeAttendances.GetByIdAsync(id);
            if (record == null)
            {
                TempData["ErrorMessage"] = "Attendance record not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.EmployeeAttendances.Delete(record);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee attendance record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Report()
        {
            var records = await _unitOfWork.EmployeeAttendances.GetAllAsync();
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

            ViewData["Title"] = "Employee Attendance Report";
            return View("~/Views/EmployeeAttendance/Report.cshtml", viewModel);
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> ExportCsv()
        {
            var records = await _unitOfWork.EmployeeAttendances.GetAllAsync();
            var employees = await _unitOfWork.Employees.GetAllAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("RecordID,EmployeeName,EmployeeUsername,Status,Detection,RecordedAt,Note");

            foreach (var r in records.OrderByDescending(x => x.CreatedAt))
            {
                var emp = employees.FirstOrDefault(e => e.Id == r.EmployeeId);
                var fullname = emp?.Fullname ?? "Unknown";
                var username = emp?.Username ?? "Unknown";
                var detection = r.ByIA ? "AI Camera" : "Manual Entry";
                csvBuilder.AppendLine($"{r.Id},{fullname},{username},{r.Status},{detection},{r.CreatedAt:yyyy-MM-dd HH:mm:ss},{r.Note}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(csvBytes, "text/csv", $"employee_attendance_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public IActionResult UploadImage()
        {
            var model = new AIPictureUploadViewModel();
            ViewData["Title"] = "AI Employee Check-in Scan";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
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

            var employees = await _unitOfWork.Employees.GetAllAsync();
            var employeeIds = employees.Select(e => e.Id).ToHashSet();
            var embeddings = await _unitOfWork.FaceEmbeddings.FindAsync(e => employeeIds.Contains(e.BaseUserId));
            var knownEmbeddings = embeddings.Select(e => new EmbeddingRecord
            {
                UserId        = e.BaseUserId,
                EmbeddingJson = e.EmbeddingJson
            }).ToList();

            if (!knownEmbeddings.Any())
            {
                model.IsProcessed = true;
                model.IsSuccess = false;
                model.Message = "No employee face embeddings registered in the system.";
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
                model.Message = "No recognized employee faces matching the uploaded image (or confidence score is too low).";
                ViewData["Title"] = "AI Employee Check-in Scan";
                return View(model);
            }

            var now = DateTime.Now;
            var nowTime = now.TimeOfDay;
            var today = now.Date;

            // Load settings for employee check-in/out windows
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
            {
                model.IsProcessed = true;
                model.IsSuccess = false;
                model.UploadedImageUrl = imageUrl;
                model.Message = "Outside check-in and check-out time windows. No attendance recorded.";
                ViewData["Title"] = "AI Employee Check-in Scan";
                return View(model);
            }

            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? loggedBy = Guid.TryParse(currentUserIdStr, out Guid logId) ? logId : null;

            int recorded = 0;
            foreach (var match in confidentMatches)
            {
                var matchedEmp = employees.FirstOrDefault(e => e.Id == match.UserId);
                if (matchedEmp is null) continue;

                // Find or create today's record for this employee
                var existing = (await _unitOfWork.EmployeeAttendances.FindAsync(a =>
                    a.EmployeeId == matchedEmp.Id &&
                    a.AttendanceDate == today)).FirstOrDefault();

                if (isCheckIn)
                {
                    if (existing is null)
                    {
                        var record = new EmployeeAttendance
                        {
                            EmployeeId             = matchedEmp.Id,
                            AttendanceDate         = today,
                            CheckInTime            = nowTime,
                            Status                 = AttendanceStatus.Present,
                            ByIA                   = true,
                            CreatedBy              = loggedBy,
                            ModifiedBy             = loggedBy,
                            RecognitionConfidence  = match.Confidence,
                            Note                   = "Detected via AI face snapshot upload scan."
                        };
                        await _unitOfWork.EmployeeAttendances.AddAsync(record);
                        recorded++;
                    }
                }
                else if (isCheckOut && existing is not null && existing.CheckOutTime is null)
                {
                    existing.CheckOutTime           = nowTime;
                    existing.RecognitionConfidence  = match.Confidence;
                    existing.ModifiedBy             = loggedBy;
                    _unitOfWork.EmployeeAttendances.Update(existing);
                    recorded++;
                }

                model.Matches.Add(new MatchedCandidateViewModel
                {
                    Name = matchedEmp.Fullname,
                    Confidence = Math.Round(match.Confidence * 100.0, 1),
                    BoundingBox = match.BoundingBox,
                    Role = "Employee"
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
                model.Message = "No registered employees identified in this image.";
                ViewData["Title"] = "AI Employee Check-in Scan";
                return View(model);
            }

            // Set backward compatible fields to the best match
            var bestMatch = model.Matches.OrderByDescending(m => m.Confidence).First();
            model.MatchedName = bestMatch.Name;
            model.Confidence = bestMatch.Confidence;
            model.BoundingBox = bestMatch.BoundingBox;

            string eventType = isCheckIn ? "check-in" : "check-out";
            model.IsProcessed = true;
            model.IsSuccess = true;
            model.UploadedImageUrl = imageUrl;
            model.Message = $"Recognized {model.Matches.Count} face(s) successfully. Employee {eventType} recorded for {recorded} employee(s).";

            ViewData["Title"] = "AI Employee Check-in Scan";
            return View(model);
        }

        private async Task PopulateFilterOptions(EmployeeAttendanceFilterViewModel model)
        {
            var branches = await _unitOfWork.Branches.GetAllAsync();

            model.BranchOptions = branches.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name }).ToList();
            model.StatusOptions = Enum.GetValues(typeof(AttendanceStatus)).Cast<AttendanceStatus>().Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.ToString()
            }).ToList();
        }

        private async Task PopulateFormOptions(EmployeeAttendanceFormViewModel model)
        {
            var employees = await _unitOfWork.Employees.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();

            model.EmployeeOptions = employees.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.Fullname} ({e.Username})"
            }).ToList();

            model.CameraOptions = cameras.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Title
            }).ToList();

            model.StatusOptions = Enum.GetValues(typeof(AttendanceStatus)).Cast<AttendanceStatus>().Select(s => new SelectListItem
            {
                Value = s.ToString(),
                Text = s.ToString()
            }).ToList();
        }
    }
}
