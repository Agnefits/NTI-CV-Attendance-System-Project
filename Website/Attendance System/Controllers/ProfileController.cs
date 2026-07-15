using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Helpers;
using Attendance_System.Services.Interfaces;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Profile;

namespace Attendance_System.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public ProfileController(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out Guid userId))
            {
                return RedirectToAction("Logout", "Auth");
            }

            var user = await _unitOfWork.BaseUsers.GetByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var viewModel = new ProfileViewModel
            {
                Username = user.Username,
                Fullname = user.Role.ToString(), // Default role if subclasses not found
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ImageUrl = user.ImageUrl,
                Role = user.Role.ToString()
            };

            // Attempt to get specific full name based on TPT subtype
            if (user.Role == Models.Enums.Roles.Admin)
            {
                var admin = await _unitOfWork.AdminUsers.GetByIdAsync(userId);
                if (admin != null) viewModel.Fullname = admin.Fullname;
            }
            else if (user.Role == Models.Enums.Roles.Employee || user.Role == Models.Enums.Roles.Teacher)
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(userId);
                if (employee != null) viewModel.Fullname = employee.Fullname;
            }
            else if (user.Role == Models.Enums.Roles.Student)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(userId);
                if (student != null) viewModel.Fullname = student.Fullname;
            }

            ViewData["Title"] = "My Profile";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out Guid userId))
            {
                return RedirectToAction("Logout", "Auth");
            }

            var user = await _unitOfWork.BaseUsers.GetByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var fullname = "";
            if (user.Role == Models.Enums.Roles.Admin)
            {
                var admin = await _unitOfWork.AdminUsers.GetByIdAsync(userId);
                fullname = admin?.Fullname ?? "";
            }
            else if (user.Role == Models.Enums.Roles.Employee || user.Role == Models.Enums.Roles.Teacher)
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(userId);
                fullname = employee?.Fullname ?? "";
            }
            else if (user.Role == Models.Enums.Roles.Student)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(userId);
                fullname = student?.Fullname ?? "";
            }

            var viewModel = new EditProfileViewModel
            {
                Fullname = fullname,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ImageUrl = user.ImageUrl
            };

            ViewData["Title"] = "Edit Profile";
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out Guid userId))
            {
                return RedirectToAction("Logout", "Auth");
            }

            var user = await _unitOfWork.BaseUsers.GetByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // Upload image file if provided
            if (model.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(user.ImageUrl))
                {
                    _fileService.DeleteFile(user.ImageUrl);
                }
                user.ImageUrl = await _fileService.UploadFileAsync(model.ImageFile, "uploads/avatars");
            }

            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            _unitOfWork.BaseUsers.Update(user);

            // Update specific table for Fullname
            if (user.Role == Models.Enums.Roles.Admin)
            {
                var admin = await _unitOfWork.AdminUsers.GetByIdAsync(userId);
                if (admin != null)
                {
                    admin.Fullname = model.Fullname;
                    _unitOfWork.AdminUsers.Update(admin);
                }
            }
            else if (user.Role == Models.Enums.Roles.Employee || user.Role == Models.Enums.Roles.Teacher)
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(userId);
                if (employee != null)
                {
                    employee.Fullname = model.Fullname;
                    _unitOfWork.Employees.Update(employee);
                }
            }
            else if (user.Role == Models.Enums.Roles.Student)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(userId);
                if (student != null)
                {
                    student.Fullname = model.Fullname;
                    _unitOfWork.Students.Update(student);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile details updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Failed to change password. Please verify input requirements.";
                return RedirectToAction("Index");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out Guid userId))
            {
                return RedirectToAction("Logout", "Auth");
            }

            var user = await _unitOfWork.BaseUsers.GetByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.Password))
            {
                TempData["ErrorMessage"] = "The current password entered is incorrect.";
                return RedirectToAction("Index");
            }

            user.Password = PasswordHelper.HashPassword(model.NewPassword);
            _unitOfWork.BaseUsers.Update(user);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction("Index");
        }
    }
}
