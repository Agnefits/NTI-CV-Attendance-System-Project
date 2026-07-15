using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

            var students = await _unitOfWork.Students.GetAllAsync();
            var employees = await _unitOfWork.Employees.GetAllAsync();
            var cameras = await _unitOfWork.Cameras.GetAllAsync();
            var baseUsers = await _unitOfWork.BaseUsers.GetAllAsync();

            var studToday = await _unitOfWork.StudentAttendances.FindAsync(a => a.CreatedAt.Date == today);
            var empToday = await _unitOfWork.EmployeeAttendances.FindAsync(a => a.CreatedAt.Date == today);

            var viewModel = new DashboardViewModel
            {
                TotalStudents = students.Count(),
                TotalCameras = cameras.Count()
            };

            // Calculate aggregated metrics for today
            viewModel.PresentToday = studToday.Count(a => a.Status == AttendanceStatus.Present) + empToday.Count(a => a.Status == AttendanceStatus.Present);
            viewModel.AbsentToday = studToday.Count(a => a.Status == AttendanceStatus.Absent) + empToday.Count(a => a.Status == AttendanceStatus.Absent);
            viewModel.LateToday = studToday.Count(a => a.Status == AttendanceStatus.Late) + empToday.Count(a => a.Status == AttendanceStatus.Late);

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

            ViewData["Title"] = "Dashboard Analytics";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetAttendanceChart()
        {
            var chartData = new AttendanceChartDataViewModel();
            var today = DateTime.Today;

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
