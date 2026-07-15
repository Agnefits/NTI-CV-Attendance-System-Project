using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Branch;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class BranchController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public BranchController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var branches = await _unitOfWork.Branches.GetAllAsync();
            var admins = await _unitOfWork.AdminUsers.GetAllAsync();
            var employees = await _unitOfWork.Employees.GetAllAsync();

            var viewModel = branches.Select(b => new BranchListViewModel
            {
                Id = b.Id,
                Name = b.Name,
                Location = b.Location,
                EmployeeCount = employees.Count(e => e.BranchId == b.Id) + admins.Count(a => a.BranchId == b.Id)
            }).ToList();

            ViewData["Title"] = "Branches";
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Branch";
            return View(new BranchFormViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(BranchFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var branch = new Branch
            {
                Name = model.Name,
                Location = model.Location
            };

            await _unitOfWork.Branches.AddAsync(branch);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Branch added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null)
            {
                TempData["ErrorMessage"] = "Branch not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new BranchFormViewModel
            {
                Id = branch.Id,
                Name = branch.Name,
                Location = branch.Location
            };

            ViewData["Title"] = "Edit Branch";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BranchFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var branch = await _unitOfWork.Branches.GetByIdAsync(model.Id.GetValueOrDefault());
            if (branch == null)
            {
                TempData["ErrorMessage"] = "Branch not found.";
                return RedirectToAction(nameof(Index));
            }

            branch.Name = model.Name;
            branch.Location = model.Location;

            _unitOfWork.Branches.Update(branch);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Branch updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null)
            {
                TempData["ErrorMessage"] = "Branch not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.Branches.Delete(branch);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Branch deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
