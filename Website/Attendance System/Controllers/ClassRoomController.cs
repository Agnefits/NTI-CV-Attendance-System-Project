using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.ClassRoom;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class ClassRoomController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClassRoomController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rooms = await _unitOfWork.ClassRooms.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();

            var viewModel = rooms.Select(r => new ClassRoomListViewModel
            {
                Id = r.Id,
                Title = r.Title,
                Location = r.Location,
                Notes = r.Notes,
                CameraCount = cameras.Count(c => c.ClassRoomId == r.Id)
            }).ToList();

            ViewData["Title"] = "Classrooms";
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Classroom";
            return View(new ClassRoomFormViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(ClassRoomFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var room = new ClassRoom
            {
                Title = model.Title,
                Location = model.Location,
                Notes = model.Notes
            };

            await _unitOfWork.ClassRooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Classroom added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var room = await _unitOfWork.ClassRooms.GetByIdAsync(id);
            if (room == null)
            {
                TempData["ErrorMessage"] = "Classroom not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ClassRoomFormViewModel
            {
                Id = room.Id,
                Title = room.Title,
                Location = room.Location,
                Notes = room.Notes
            };

            ViewData["Title"] = "Edit Classroom";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ClassRoomFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var room = await _unitOfWork.ClassRooms.GetByIdAsync(model.Id.GetValueOrDefault());
            if (room == null)
            {
                TempData["ErrorMessage"] = "Classroom not found.";
                return RedirectToAction(nameof(Index));
            }

            room.Title = model.Title;
            room.Location = model.Location;
            room.Notes = model.Notes;

            _unitOfWork.ClassRooms.Update(room);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Classroom updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var room = await _unitOfWork.ClassRooms.GetByIdAsync(id);
            if (room == null)
            {
                TempData["ErrorMessage"] = "Classroom not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.ClassRooms.Delete(room);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Classroom deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
