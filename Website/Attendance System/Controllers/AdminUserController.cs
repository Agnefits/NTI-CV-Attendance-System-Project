using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Attributes;
using Attendance_System.Helpers;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.AdminUser;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class AdminUserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminUserController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index(AdminUserFilterViewModel filter)
        {
            var admins = await _unitOfWork.AdminUsers.GetAllAsync();
            var branches = await _unitOfWork.Branches.GetAllAsync();

            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                admins = admins.Where(a => a.Fullname.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                           a.Username.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                           (a.Email != null && a.Email.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase)));
            }
            if (filter.BranchId.HasValue)
            {
                admins = admins.Where(a => a.BranchId == filter.BranchId.Value);
            }

            filter.Results = admins.Select(a => new AdminUserListViewModel
            {
                Id = a.Id,
                Username = a.Username,
                Fullname = a.Fullname,
                Email = a.Email,
                PhoneNumber = a.PhoneNumber,
                BranchName = branches.FirstOrDefault(b => b.Id == a.BranchId)?.Name ?? "All Branches / General"
            }).ToList();

            filter.BranchOptions = branches.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name }).ToList();

            ViewData["Title"] = "Administrators";
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new AdminUserFormViewModel();
            await PopulateBranches(model);

            ViewData["Title"] = "Add Administrator";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AdminUserFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBranches(model);
                return View(model);
            }

            // Check username duplicate
            var existingUser = await _unitOfWork.BaseUsers.GetByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Username), "Username is already in use.");
                await PopulateBranches(model);
                return View(model);
            }

            var admin = new AdminUser
            {
                Username = model.Username,
                Password = PasswordHelper.HashPassword(model.Password),
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Fullname = model.Fullname,
                BranchId = model.BranchId
            };

            await _unitOfWork.AdminUsers.AddAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Administrator registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var admin = await _unitOfWork.AdminUsers.GetByIdAsync(id);
            if (admin == null)
            {
                TempData["ErrorMessage"] = "Administrator not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new AdminUserFormViewModel
            {
                Id = admin.Id,
                Username = admin.Username,
                Email = admin.Email,
                PhoneNumber = admin.PhoneNumber,
                Fullname = admin.Fullname,
                BranchId = admin.BranchId
            };

            await PopulateBranches(model);

            ViewData["Title"] = "Edit Administrator";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AdminUserFormViewModel model)
        {
            ModelState.Remove(nameof(model.Password));

            if (!ModelState.IsValid)
            {
                await PopulateBranches(model);
                return View(model);
            }

            var admin = await _unitOfWork.AdminUsers.GetByIdAsync(model.Id.GetValueOrDefault());
            if (admin == null)
            {
                TempData["ErrorMessage"] = "Administrator not found.";
                return RedirectToAction(nameof(Index));
            }

            admin.Username = model.Username;
            admin.Email = model.Email;
            admin.PhoneNumber = model.PhoneNumber;
            admin.Fullname = model.Fullname;
            admin.BranchId = model.BranchId;

            _unitOfWork.AdminUsers.Update(admin);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Administrator updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Protect current logged-in user from self-deletion
            var selfId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (id == selfId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own administrator account.";
                return RedirectToAction(nameof(Index));
            }

            var admin = await _unitOfWork.AdminUsers.GetByIdAsync(id);
            if (admin == null)
            {
                TempData["ErrorMessage"] = "Administrator not found.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.AdminUsers.Delete(admin);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Administrator deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBranches(AdminUserFormViewModel model)
        {
            var branches = await _unitOfWork.Branches.GetAllAsync();
            model.BranchOptions = branches.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            }).ToList();
        }
    }
}
