using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Level;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class LevelController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public LevelController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var students = await _unitOfWork.Students.GetAllAsync();

            var viewModel = levels.Select(l => new LevelListViewModel
            {
                Id = l.Id,
                Title = l.Title,
                ClassCount = classes.Count(c => c.LevelId == l.Id),
                StudentCount = students.Count(s => s.LevelId == l.Id)
            }).ToList();

            ViewData["Title"] = "Academic Levels";
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Level";
            return View(new LevelFormViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(LevelFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var level = new Level { Title = model.Title };

            await _unitOfWork.Levels.AddAsync(level);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Academic level added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var level = await _unitOfWork.Levels.GetByIdAsync(id);
            if (level == null)
            {
                TempData["ErrorMessage"] = "Academic level not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new LevelFormViewModel
            {
                Id = level.Id,
                Title = level.Title
            };

            ViewData["Title"] = "Edit Level";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(LevelFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var level = await _unitOfWork.Levels.GetByIdAsync(model.Id.GetValueOrDefault());
            if (level == null)
            {
                TempData["ErrorMessage"] = "Academic level not found.";
                return RedirectToAction(nameof(Index));
            }

            level.Title = model.Title;

            _unitOfWork.Levels.Update(level);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Academic level updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var level = await _unitOfWork.Levels.GetByIdAsync(id);
            if (level == null)
            {
                TempData["ErrorMessage"] = "Academic level not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.Levels.Delete(level);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Academic level deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
