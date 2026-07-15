using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Attendance;
using Attendance_System.ViewModels.Dashboard;

namespace Attendance_System.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            var today = DateTime.Today;
            var viewModel = new DashboardViewModel();

            if (userRoleClaim == Roles.Admin.ToString())
            {
                var students = await _unitOfWork.Students.GetAllAsync();
                var employees = await _unitOfWork.Employees.GetAllAsync();
                var cameras = await _unitOfWork.Cameras.GetAllAsync();

                var studToday = await _unitOfWork.StudentAttendances.FindAsync(a => a.CreatedAt.Date == today);
                var empToday = await _unitOfWork.EmployeeAttendances.FindAsync(a => a.CreatedAt.Date == today);

                viewModel.TotalStudents = students.Count();
                viewModel.TotalCameras = cameras.Count();
                viewModel.PresentToday = studToday.Count(a => a.Status == AttendanceStatus.Present) + empToday.Count(a => a.Status == AttendanceStatus.Present);
                viewModel.AbsentToday = studToday.Count(a => a.Status == AttendanceStatus.Absent) + empToday.Count(a => a.Status == AttendanceStatus.Absent);
                viewModel.LateToday = studToday.Count(a => a.Status == AttendanceStatus.Late) + empToday.Count(a => a.Status == AttendanceStatus.Late);
                viewModel.Role = "Admin";

                // Fetch recent student attendances
                var recentStud = (await _unitOfWork.StudentAttendances.GetAllAsync())
                    .Select(r => {
                        var s = students.FirstOrDefault(st => st.Id == r.StudentId);
                        return new AttendanceListViewModel
                        {
                            Id = r.Id,
                            UserId = r.StudentId,
                            UserFullname = s?.Fullname ?? "Unknown",
                            UserType = "Student",
                            Status = r.Status,
                            ByIA = r.ByIA,
                            CameraTitle = cameras.FirstOrDefault(c => c.Id == r.CameraId)?.Title,
                            CreatedAt = r.CreatedAt,
                            Note = r.Note
                        };
                    });

                // Fetch recent employee attendances
                var recentEmp = (await _unitOfWork.EmployeeAttendances.GetAllAsync())
                    .Select(r => {
                        var emp = employees.FirstOrDefault(e => e.Id == r.EmployeeId);
                        return new AttendanceListViewModel
                        {
                            Id = r.Id,
                            UserId = r.EmployeeId,
                            UserFullname = emp?.Fullname ?? "Unknown",
                            UserType = "Employee",
                            Status = r.Status,
                            ByIA = r.ByIA,
                            CameraTitle = cameras.FirstOrDefault(c => c.Id == r.CameraId)?.Title,
                            CreatedAt = r.CreatedAt,
                            Note = r.Note
                        };
                    });

                viewModel.RecentAttendances = recentStud.Concat(recentEmp)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToList();
            }
            else if (userRoleClaim == Roles.Student.ToString() && Guid.TryParse(userIdClaim, out Guid studentId))
            {
                var studentObj = await _unitOfWork.Students.GetByIdAsync(studentId);
                var studentRecords = await _unitOfWork.StudentAttendances.FindAsync(a => a.StudentId == studentId);
                var cameras = await _unitOfWork.Cameras.GetAllAsync();

                viewModel.Role = "Student";
                viewModel.TotalLessonsOrDays = studentRecords.Count();
                viewModel.AttendedCount = studentRecords.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
                viewModel.AttendanceRate = viewModel.TotalLessonsOrDays > 0 
                    ? Math.Round((double)viewModel.AttendedCount / viewModel.TotalLessonsOrDays * 100, 1) 
                    : 100.0;

                viewModel.PresentToday = studentRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Present);
                viewModel.LateToday = studentRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Late);
                viewModel.AbsentToday = studentRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Absent);

                viewModel.RecentAttendances = studentRecords
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new AttendanceListViewModel
                    {
                        Id = r.Id,
                        UserId = r.StudentId,
                        UserFullname = studentObj?.Fullname ?? "My Profile",
                        UserType = "Student",
                        Status = r.Status,
                        ByIA = r.ByIA,
                        CameraTitle = cameras.FirstOrDefault(c => c.Id == r.CameraId)?.Title,
                        CreatedAt = r.CreatedAt,
                        Note = r.Note
                    }).ToList();
            }
            else if (userRoleClaim == Roles.Employee.ToString() && Guid.TryParse(userIdClaim, out Guid employeeId))
            {
                var employeeObj = await _unitOfWork.Employees.GetByIdAsync(employeeId);
                var employeeRecords = await _unitOfWork.EmployeeAttendances.FindAsync(a => a.EmployeeId == employeeId);
                var cameras = await _unitOfWork.Cameras.GetAllAsync();

                viewModel.Role = "Employee";
                viewModel.TotalLessonsOrDays = employeeRecords.Count();
                viewModel.AttendedCount = employeeRecords.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
                viewModel.AttendanceRate = viewModel.TotalLessonsOrDays > 0 
                    ? Math.Round((double)viewModel.AttendedCount / viewModel.TotalLessonsOrDays * 100, 1) 
                    : 100.0;

                viewModel.PresentToday = employeeRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Present);
                viewModel.LateToday = employeeRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Late);
                viewModel.AbsentToday = employeeRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Absent);

                viewModel.RecentAttendances = employeeRecords
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new AttendanceListViewModel
                    {
                        Id = r.Id,
                        UserId = r.EmployeeId,
                        UserFullname = employeeObj?.Fullname ?? "My Profile",
                        UserType = "Employee",
                        Status = r.Status,
                        ByIA = r.ByIA,
                        CameraTitle = cameras.FirstOrDefault(c => c.Id == r.CameraId)?.Title,
                        CreatedAt = r.CreatedAt,
                        Note = r.Note
                    }).ToList();
            }
            else if (userRoleClaim == Roles.Teacher.ToString() && Guid.TryParse(userIdClaim, out Guid teacherId))
            {
                var teacherLessons = await _unitOfWork.Lessons.FindAsync(l => l.TeacherId == teacherId);
                var lessonIds = teacherLessons.Select(l => l.Id).ToHashSet();
                var studentRecords = await _unitOfWork.StudentAttendances.FindAsync(a => a.LessonId.HasValue && lessonIds.Contains(a.LessonId.Value));
                var students = await _unitOfWork.Students.GetAllAsync();
                var cameras = await _unitOfWork.Cameras.GetAllAsync();

                viewModel.Role = "Teacher";
                viewModel.TotalLessonsOrDays = teacherLessons.Count(); // Classes Taught
                viewModel.AttendedCount = studentRecords.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
                viewModel.AttendanceRate = studentRecords.Any() 
                    ? Math.Round((double)viewModel.AttendedCount / studentRecords.Count() * 100, 1) 
                    : 100.0;

                viewModel.PresentToday = studentRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Present);
                viewModel.LateToday = studentRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Late);
                viewModel.AbsentToday = studentRecords.Count(a => a.CreatedAt.Date == today && a.Status == AttendanceStatus.Absent);

                viewModel.RecentAttendances = studentRecords
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => {
                        var s = students.FirstOrDefault(st => st.Id == r.StudentId);
                        return new AttendanceListViewModel
                        {
                            Id = r.Id,
                            UserId = r.StudentId,
                            UserFullname = s?.Fullname ?? "Student",
                            UserType = "Student",
                            Status = r.Status,
                            ByIA = r.ByIA,
                            CameraTitle = cameras.FirstOrDefault(c => c.Id == r.CameraId)?.Title,
                            CreatedAt = r.CreatedAt,
                            Note = r.Note
                        };
                    }).ToList();
            }

            ViewData["Title"] = "Dashboard Overview";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetAttendanceChart()
        {
            var chartData = new AttendanceChartDataViewModel();
            var today = DateTime.Today;

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != Roles.Admin.ToString())
            {
                return Json(chartData);
            }

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                chartData.Labels.Add(targetDate.ToString("ddd (dd MMM)"));

                var studAttendances = await _unitOfWork.StudentAttendances.FindAsync(a => a.CreatedAt.Date == targetDate.Date);
                var empAttendances = await _unitOfWork.EmployeeAttendances.FindAsync(a => a.CreatedAt.Date == targetDate.Date);

                chartData.PresentData.Add(studAttendances.Count(a => a.Status == AttendanceStatus.Present) + empAttendances.Count(a => a.Status == AttendanceStatus.Present));
                chartData.AbsentData.Add(studAttendances.Count(a => a.Status == AttendanceStatus.Absent) + empAttendances.Count(a => a.Status == AttendanceStatus.Absent));
                chartData.LateData.Add(studAttendances.Count(a => a.Status == AttendanceStatus.Late) + empAttendances.Count(a => a.Status == AttendanceStatus.Late));
            }

            return Json(chartData);
        }
    }
}
