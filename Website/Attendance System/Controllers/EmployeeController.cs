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
using Attendance_System.ViewModels.Employee;
using Attendance_System.ViewModels.Lesson;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class EmployeeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public EmployeeController(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(EmployeeFilterViewModel filter)
        {
            var employees = await _unitOfWork.Employees.GetAllAsync();
            var branches = await _unitOfWork.Branches.GetAllAsync();

            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                employees = employees.Where(e => e.Fullname.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                                 e.Username.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                                 e.JobTitle.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                                 (e.Email != null && e.Email.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase)));
            }
            if (filter.BranchId.HasValue)
            {
                employees = employees.Where(e => e.BranchId == filter.BranchId.Value);
            }

            filter.Results = employees.Select(e => new EmployeeListViewModel
            {
                Id = e.Id,
                Username = e.Username,
                Fullname = e.Fullname,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                ImageUrl = e.ImageUrl,
                JobTitle = e.JobTitle,
                Speciality = e.Speciality,
                BranchName = branches.FirstOrDefault(b => b.Id == e.BranchId)?.Name ?? "Unassigned"
            }).ToList();

            filter.BranchOptions = branches.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name }).ToList();

            ViewData["Title"] = "Employees Directory";
            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(Index));
            }

            var branches = await _unitOfWork.Branches.GetAllAsync();
            var lessons = await _unitOfWork.Lessons.FindAsync(l => l.TeacherId == employee.Id);
            var levels = await _unitOfWork.Levels.GetAllAsync();
            var classes = await _unitOfWork.Classes.GetAllAsync();
            var rooms = await _unitOfWork.ClassRooms.GetAllAsync();

            var viewModel = new EmployeeDetailsViewModel
            {
                Id = employee.Id,
                Username = employee.Username,
                Fullname = employee.Fullname,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                ImageUrl = employee.ImageUrl,
                JobTitle = employee.JobTitle,
                Speciality = employee.Speciality,
                BranchName = branches.FirstOrDefault(b => b.Id == employee.BranchId)?.Name ?? "Unassigned",
                Lessons = lessons.Select(l => new LessonListViewModel
                {
                    Id = l.Id,
                    LevelTitle = levels.FirstOrDefault(lvl => lvl.Id == l.LevelId)?.Title ?? "Unknown",
                    ClassTitle = classes.FirstOrDefault(c => c.Id == l.ClassId)?.Title ?? "Unknown",
                    TeacherName = employee.Fullname,
                    ClassRoomTitle = rooms.FirstOrDefault(r => r.Id == l.ClassRoomId)?.Title ?? "None"
                }).ToList()
            };

            ViewData["Title"] = $"{employee.Fullname} Details";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new EmployeeFormViewModel();
            await PopulateBranches(model);

            ViewData["Title"] = "Add Employee";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeFormViewModel model)
        {
            // Detect if the view is requesting a JSON response (2-step enroll flow)
            bool wantsJson = Request.Form.ContainsKey("returnJson");

            if (!ModelState.IsValid)
            {
                if (wantsJson)
                    return Json(new { success = false, errors = ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                await PopulateBranches(model);
                return View(model);
            }

            // Check if username is taken
            var existingUser = await _unitOfWork.BaseUsers.GetByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Username), "Username is already in use.");
                if (wantsJson)
                    return Json(new { success = false, errors = new[] { "Username is already in use." } });
                await PopulateBranches(model);
                return View(model);
            }

            var employee = new Employee
            {
                Username = model.Username,
                Password = PasswordHelper.HashPassword(model.Password),
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Fullname = model.Fullname,
                JobTitle = model.JobTitle,
                Speciality = model.Speciality,
                BranchId = model.BranchId
            };

            // Set role dynamically to Teacher if JobTitle indicates Teacher
            if (model.JobTitle.Equals("Teacher", StringComparison.OrdinalIgnoreCase) || 
                model.JobTitle.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
            {
                employee.Role = Roles.Teacher;
            }
            else
            {
                employee.Role = Roles.Employee;
            }

            if (model.ImageFile != null)
            {
                employee.ImageUrl = await _fileService.UploadFileAsync(model.ImageFile, "uploads/avatars");
            }

            await _unitOfWork.Employees.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            // Return JSON with the new employee's ID so the enroll panel can use it
            if (wantsJson)
                return Json(new { success = true, userId = employee.Id });

            TempData["SuccessMessage"] = "Employee added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new EmployeeFormViewModel
            {
                Id = employee.Id,
                Username = employee.Username,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                Fullname = employee.Fullname,
                JobTitle = employee.JobTitle,
                Speciality = employee.Speciality,
                BranchId = employee.BranchId,
                ImageUrl = employee.ImageUrl
            };

            await PopulateBranches(model);

            ViewData["Title"] = "Edit Employee";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeFormViewModel model)
        {
            ModelState.Remove(nameof(model.Password));

            if (!ModelState.IsValid)
            {
                await PopulateBranches(model);
                return View(model);
            }

            var employee = await _unitOfWork.Employees.GetByIdAsync(model.Id.GetValueOrDefault());
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(Index));
            }

            employee.Username = model.Username;
            employee.Email = model.Email;
            employee.PhoneNumber = model.PhoneNumber;
            employee.Fullname = model.Fullname;
            employee.JobTitle = model.JobTitle;
            employee.Speciality = model.Speciality;
            employee.BranchId = model.BranchId;

            // Set role dynamically to Teacher if JobTitle indicates Teacher
            if (model.JobTitle.Equals("Teacher", StringComparison.OrdinalIgnoreCase) || 
                model.JobTitle.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
            {
                employee.Role = Roles.Teacher;
            }
            else
            {
                employee.Role = Roles.Employee;
            }

            if (model.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(employee.ImageUrl))
                {
                    _fileService.DeleteFile(employee.ImageUrl);
                }
                employee.ImageUrl = await _fileService.UploadFileAsync(model.ImageFile, "uploads/avatars");
            }

            _unitOfWork.Employees.Update(employee);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee details updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(employee.ImageUrl))
            {
                _fileService.DeleteFile(employee.ImageUrl);
            }

            _unitOfWork.Employees.Delete(employee);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBranches(EmployeeFormViewModel model)
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
