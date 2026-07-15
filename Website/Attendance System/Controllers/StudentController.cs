using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Attributes;
using Attendance_System.Helpers;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.Services.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Attendance;
using Attendance_System.ViewModels.Student;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin, Roles.Teacher)]
    public class StudentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public StudentController(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(StudentFilterViewModel filter)
        {
            var students = await _unitOfWork.Students.GetAllAsync();
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var attendances = await _unitOfWork.StudentAttendances.GetAllAsync();

            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                students = students.Where(s => s.Fullname.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                               s.Username.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                               (s.Email != null && s.Email.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase)));
            }
            if (filter.LevelId.HasValue)
            {
                students = students.Where(s => s.LevelId == filter.LevelId.Value);
            }
            if (filter.ClassId.HasValue)
            {
                students = students.Where(s => s.ClassId == filter.ClassId.Value);
            }

            filter.Results = students.Select(s => {
                var studentAttendances = attendances.Where(a => a.StudentId == s.Id).ToList();
                var total = studentAttendances.Count;
                var present = studentAttendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
                var percentage = total > 0 ? Math.Round((double)present / total * 100, 1) : 100.0;

                return new StudentListViewModel
                {
                    Id = s.Id,
                    Username = s.Username,
                    Fullname = s.Fullname,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    ImageUrl = s.ImageUrl,
                    LevelTitle = levels.FirstOrDefault(l => l.Id == s.LevelId)?.Title ?? "None",
                    ClassTitle = classes.FirstOrDefault(c => c.Id == s.ClassId)?.Title ?? "None",
                    AttendancePercent = percentage
                };
            }).ToList();

            filter.LevelOptions = levels.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Title }).ToList();
            filter.ClassOptions = classes.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToList();

            ViewData["Title"] = "Students Roster";
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(id);
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(Index));
            }

            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var attendances = await _unitOfWork.StudentAttendances.FindAsync(a => a.StudentId == student.Id);
            var cameras = await _unitOfWork.Cameras.GetAllAsync();
            var embeddings = await _unitOfWork.FaceEmbeddings.FindAsync(fe => fe.BaseUserId == student.Id);

            var total = attendances.Count();
            var present = attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            var percentage = total > 0 ? Math.Round((double)present / total * 100, 1) : 100.0;

            var viewModel = new StudentDetailsViewModel
            {
                Id = student.Id,
                Username = student.Username,
                Fullname = student.Fullname,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                ImageUrl = student.ImageUrl,
                LevelTitle = levels.FirstOrDefault(l => l.Id == student.LevelId)?.Title ?? "None",
                ClassTitle = classes.FirstOrDefault(c => c.Id == student.ClassId)?.Title ?? "None",
                AttendancePercent = percentage,
                HasFaceEmbedding = embeddings.Any(),
                RecentAttendances = attendances.Select(a => new AttendanceListViewModel
                {
                    Id = a.Id,
                    Status = a.Status,
                    ByIA = a.ByIA,
                    CameraTitle = cameras.FirstOrDefault(c => c.Id == a.CameraId)?.Title ?? "System Console",
                    CreatedAt = a.CreatedAt,
                    Note = a.Note
                }).OrderByDescending(a => a.CreatedAt).Take(10).ToList()
            };

            ViewData["Title"] = $"{student.Fullname} Profile";
            return View(viewModel);
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Create()
        {
            var model = new StudentFormViewModel();
            await PopulateDropdowns(model);

            ViewData["Title"] = "Register Student";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Create(StudentFormViewModel model)
        {
            // Detect if the view is requesting a JSON response (2-step enroll flow)
            bool wantsJson = Request.Form.ContainsKey("returnJson");

            if (!ModelState.IsValid)
            {
                if (wantsJson)
                    return Json(new { success = false, errors = ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                await PopulateDropdowns(model);
                return View(model);
            }

            // Check if username is taken
            var existingUser = await _unitOfWork.BaseUsers.GetByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Username), "Username is already in use.");
                if (wantsJson)
                    return Json(new { success = false, errors = new[] { "Username is already in use." } });
                await PopulateDropdowns(model);
                return View(model);
            }

            var student = new Student
            {
                Username = model.Username,
                Password = PasswordHelper.HashPassword(model.Password),
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Fullname = model.Fullname,
                LevelId = model.LevelId,
                ClassId = model.ClassId
            };

            if (model.ImageFile != null)
            {
                student.ImageUrl = await _fileService.UploadFileAsync(model.ImageFile, "uploads/avatars");
            }

            await _unitOfWork.Students.AddAsync(student);
            await _unitOfWork.SaveChangesAsync();

            // Return JSON with the new student's ID so the enroll panel can use it
            if (wantsJson)
                return Json(new { success = true, userId = student.Id });

            TempData["SuccessMessage"] = "Student registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(id);
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new StudentFormViewModel
            {
                Id = student.Id,
                Username = student.Username,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                Fullname = student.Fullname,
                LevelId = student.LevelId,
                ClassId = student.ClassId,
                ImageUrl = student.ImageUrl
            };

            await PopulateDropdowns(model);

            ViewData["Title"] = "Edit Student";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Edit(StudentFormViewModel model)
        {
            // Remove password requirement when editing
            ModelState.Remove(nameof(model.Password));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var student = await _unitOfWork.Students.GetByIdAsync(model.Id.GetValueOrDefault());
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(Index));
            }

            student.Username = model.Username;
            student.Email = model.Email;
            student.PhoneNumber = model.PhoneNumber;
            student.Fullname = model.Fullname;
            student.LevelId = model.LevelId;
            student.ClassId = model.ClassId;

            if (model.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(student.ImageUrl))
                {
                    _fileService.DeleteFile(student.ImageUrl);
                }
                student.ImageUrl = await _fileService.UploadFileAsync(model.ImageFile, "uploads/avatars");
            }

            _unitOfWork.Students.Update(student);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(id);
            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(student.ImageUrl))
            {
                _fileService.DeleteFile(student.ImageUrl);
            }

            _unitOfWork.Students.Delete(student);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(StudentFormViewModel model)
        {
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();

            model.LevelOptions = levels.Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = l.Title
            }).ToList();

            model.ClassOptions = classes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Title
            }).ToList();
        }
    }
}
