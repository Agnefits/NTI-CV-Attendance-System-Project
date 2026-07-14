using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Class;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class ClassController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClassController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index(ClassFilterViewModel filter)
        {
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var students = await _unitOfWork.Students.GetAllAsync();

            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                classes = classes.Where(c => c.Title.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase));
            }
            if (filter.LevelId.HasValue)
            {
                classes = classes.Where(c => c.LevelId == filter.LevelId.Value);
            }

            filter.Results = classes.Select(c => new ClassListViewModel
            {
                Id = c.Id,
                Title = c.Title,
                LevelTitle = levels.FirstOrDefault(l => l.Id == c.LevelId)?.Title ?? "Unknown",
                StudentCount = students.Count(s => s.ClassId == c.Id)
            }).ToList();

            filter.LevelOptions = levels.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Title }).ToList();

            ViewData["Title"] = "Classes";
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ClassFormViewModel();
            await PopulateLevels(model);

            ViewData["Title"] = "Add Class";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ClassFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLevels(model);
                return View(model);
            }

            var newClass = new Class
            {
                Title = model.Title,
                LevelId = model.LevelId
            };

            await _unitOfWork.Classes.AddAsync(newClass);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Class added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var targetClass = await _unitOfWork.Classes.GetByIdAsync(id);
            if (targetClass == null)
            {
                TempData["ErrorMessage"] = "Class not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ClassFormViewModel
            {
                Id = targetClass.Id,
                Title = targetClass.Title,
                LevelId = targetClass.LevelId
            };

            await PopulateLevels(model);

            ViewData["Title"] = "Edit Class";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ClassFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLevels(model);
                return View(model);
            }

            var targetClass = await _unitOfWork.Classes.GetByIdAsync(model.Id.GetValueOrDefault());
            if (targetClass == null)
            {
                TempData["ErrorMessage"] = "Class not found.";
                return RedirectToAction(nameof(Index));
            }

            targetClass.Title = model.Title;
            targetClass.LevelId = model.LevelId;

            _unitOfWork.Classes.Update(targetClass);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Class updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var targetClass = await _unitOfWork.Classes.GetByIdAsync(id);
            if (targetClass == null)
            {
                TempData["ErrorMessage"] = "Class not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.Classes.Delete(targetClass);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Class deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateLevels(ClassFormViewModel model)
        {
            var levels = await _unitOfWork.Levels.GetAllAsync();
            model.LevelOptions = levels.Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = l.Title
            }).ToList();
        }
    }
}
