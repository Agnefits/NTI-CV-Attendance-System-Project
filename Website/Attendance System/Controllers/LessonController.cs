using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Lesson;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin, Roles.Teacher, Roles.Student)]
    public class LessonController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public LessonController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index(LessonFilterViewModel filter)
        {
            var lessons = await _unitOfWork.Lessons.GetAllAsync();
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var teachers = await _unitOfWork.Employees.GetAllAsync();
            var rooms = await _unitOfWork.ClassRooms.GetAllAsync();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole == Roles.Student.ToString() && Guid.TryParse(userIdStr, out Guid studentId))
            {
                var student = await _unitOfWork.Students.GetByIdAsync(studentId);
                if (student != null)
                {
                    lessons = lessons.Where(l => l.LevelId == student.LevelId && (l.ClassId == null || l.ClassId == student.ClassId));
                }
                else
                {
                    lessons = Enumerable.Empty<Lesson>();
                }
            }
            else if (userRole == Roles.Teacher.ToString() && Guid.TryParse(userIdStr, out Guid teacherId))
            {
                lessons = lessons.Where(l => l.TeacherId == teacherId);
            }
            else
            {
                if (filter.LevelId.HasValue)
                {
                    lessons = lessons.Where(l => l.LevelId == filter.LevelId.Value);
                }
                if (filter.ClassId.HasValue)
                {
                    lessons = lessons.Where(l => l.ClassId == filter.ClassId.Value);
                }
                if (filter.TeacherId.HasValue)
                {
                    lessons = lessons.Where(l => l.TeacherId == filter.TeacherId.Value);
                }
                if (filter.DayOfWeek.HasValue)
                {
                    lessons = lessons.Where(l => l.DayOfWeek == filter.DayOfWeek.Value);
                }
            }

            filter.Results = lessons.Select(l => new LessonListViewModel
            {
                Id = l.Id,
                LevelId = l.LevelId,
                LevelTitle = levels.FirstOrDefault(lvl => lvl.Id == l.LevelId)?.Title ?? "Unknown",
                ClassId = l.ClassId,
                ClassTitle = classes.FirstOrDefault(c => c.Id == l.ClassId)?.Title ?? "General (No Class)",
                TeacherId = l.TeacherId,
                TeacherName = teachers.FirstOrDefault(t => t.Id == l.TeacherId)?.Fullname ?? "Unknown",
                ClassRoomId = l.ClassRoomId,
                ClassRoomTitle = rooms.FirstOrDefault(r => r.Id == l.ClassRoomId)?.Title ?? "None",
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                DayOfWeek = l.DayOfWeek,
                StartDate = l.StartDate,
                EndDate = l.EndDate
            }).ToList();

            if (userRole == Roles.Admin.ToString())
            {
                await PopulateFilterOptions(filter);
            }

            ViewData["Title"] = "Lessons List";
            return View(filter);
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Create()
        {
            var model = new LessonFormViewModel();
            await PopulateFormOptions(model);

            ViewData["Title"] = "Add Lesson";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Create(LessonFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormOptions(model);
                return View(model);
            }

            var lesson = new Lesson
            {
                LevelId = model.LevelId,
                ClassId = model.ClassId,
                TeacherId = model.TeacherId,
                ClassRoomId = model.ClassRoomId,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                DayOfWeek = model.DayOfWeek,
                StartDate = model.StartDate,
                EndDate = model.EndDate
            };

            await _unitOfWork.Lessons.AddAsync(lesson);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lesson schedule added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
            {
                TempData["ErrorMessage"] = "Lesson not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new LessonFormViewModel
            {
                Id = lesson.Id,
                LevelId = lesson.LevelId,
                ClassId = lesson.ClassId,
                TeacherId = lesson.TeacherId,
                ClassRoomId = lesson.ClassRoomId,
                StartTime = lesson.StartTime,
                EndTime = lesson.EndTime,
                DayOfWeek = lesson.DayOfWeek,
                StartDate = lesson.StartDate,
                EndDate = lesson.EndDate
            };

            await PopulateFormOptions(model);

            ViewData["Title"] = "Edit Lesson";
            return View(model);
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Edit(LessonFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFormOptions(model);
                return View(model);
            }

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(model.Id.GetValueOrDefault());
            if (lesson == null)
            {
                TempData["ErrorMessage"] = "Lesson not found.";
                return RedirectToAction(nameof(Index));
            }

            lesson.LevelId = model.LevelId;
            lesson.ClassId = model.ClassId;
            lesson.TeacherId = model.TeacherId;
            lesson.ClassRoomId = model.ClassRoomId;
            lesson.StartTime = model.StartTime;
            lesson.EndTime = model.EndTime;
            lesson.DayOfWeek = model.DayOfWeek;
            lesson.StartDate = model.StartDate;
            lesson.EndDate = model.EndDate;

            _unitOfWork.Lessons.Update(lesson);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lesson schedule updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AuthorizedRoles(Roles.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
            {
                TempData["ErrorMessage"] = "Lesson not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.Lessons.Delete(lesson);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lesson deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateFilterOptions(LessonFilterViewModel model)
        {
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var teachers = await _unitOfWork.Employees.FindAsync(e => e.Role == Roles.Teacher);

            model.LevelOptions = levels.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Title }).ToList();
            model.ClassOptions = classes.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToList();
            model.TeacherOptions = teachers.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Fullname }).ToList();

            model.DayOfWeekOptions = Enum.GetValues(typeof(DayOfWeek))
                .Cast<DayOfWeek>()
                .Select(d => new SelectListItem
                {
                    Value = d.ToString(),
                    Text = d.ToString()
                }).ToList();
        }

        private async Task PopulateFormOptions(LessonFormViewModel model)
        {
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var teachers = await _unitOfWork.Employees.FindAsync(e => e.Role == Roles.Teacher);
            var rooms = await _unitOfWork.ClassRooms.GetAllAsync();

            model.LevelOptions = levels.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Title }).ToList();
            model.ClassOptions = classes.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToList();
            model.TeacherOptions = teachers.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Fullname }).ToList();
            model.ClassRoomOptions = rooms.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Title }).ToList();

            model.DayOfWeekOptions = Enum.GetValues(typeof(DayOfWeek))
                .Cast<DayOfWeek>()
                .Select(d => new SelectListItem
                {
                    Value = d.ToString(),
                    Text = d.ToString()
                }).ToList();
        }
    }
}
