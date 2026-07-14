using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Camera;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class CameraController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CameraController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cameras = await _unitOfWork.Cameras.GetAllAsync();
            var rooms = await _unitOfWork.ClassRooms.GetAllAsync();

            var viewModel = cameras.Select(c => new CameraListViewModel
            {
                Id = c.Id,
                Title = c.Title,
                Location = c.Location,
                ClassRoomTitle = rooms.FirstOrDefault(r => r.Id == c.ClassRoomId)?.Title ?? "Unassigned",
                Key = c.Key,
                Notes = c.Notes,
                IsOnline = true // Mocked state
            }).ToList();

            ViewData["Title"] = "Cameras Monitor";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CameraFormViewModel();
            await PopulateRooms(model);

            ViewData["Title"] = "Add Camera";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CameraFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRooms(model);
                return View(model);
            }

            var camera = new Camera
            {
                Title = model.Title,
                Location = model.Location,
                ClassRoomId = model.ClassRoomId,
                Key = model.Key,
                Notes = model.Notes
            };

            await _unitOfWork.Cameras.AddAsync(camera);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Camera module registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(id);
            if (camera == null)
            {
                TempData["ErrorMessage"] = "Camera not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CameraFormViewModel
            {
                Id = camera.Id,
                Title = camera.Title,
                Location = camera.Location,
                ClassRoomId = camera.ClassRoomId,
                Key = camera.Key,
                Notes = camera.Notes
            };

            await PopulateRooms(model);

            ViewData["Title"] = "Edit Camera";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CameraFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRooms(model);
                return View(model);
            }

            var camera = await _unitOfWork.Cameras.GetByIdAsync(model.Id.GetValueOrDefault());
            if (camera == null)
            {
                TempData["ErrorMessage"] = "Camera not found.";
                return RedirectToAction(nameof(Index));
            }

            camera.Title = model.Title;
            camera.Location = model.Location;
            camera.ClassRoomId = model.ClassRoomId;
            camera.Key = model.Key;
            camera.Notes = model.Notes;

            _unitOfWork.Cameras.Update(camera);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Camera module updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(id);
            if (camera == null)
            {
                TempData["ErrorMessage"] = "Camera not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.Cameras.Delete(camera);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Camera module deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public JsonResult TestConnection(Guid id)
        {
            // Simulate check pinging
            var randomOnline = new Random().Next(0, 10) > 1; // 90% chance of online success
            return Json(new { success = true, isOnline = randomOnline });
        }

        private async Task PopulateRooms(CameraFormViewModel model)
        {
            var rooms = await _unitOfWork.ClassRooms.GetAllAsync();
            model.ClassRoomOptions = rooms.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Title
            }).ToList();
        }
    }
}
