using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Attendance_System.Attributes;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Setting;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class SettingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public SettingController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _unitOfWork.Settings.GetAllAsync();

            var viewModel = new SettingListViewModel
            {
                Settings = settings.Select(s => new SettingItemViewModel
                {
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Key switch
                    {
                        "AttendanceStartTime" => "The hour marking daily start (e.g. 08:00)",
                        "LateGracePeriodMinutes" => "Grace period minutes before a student is flagged LATE",
                        "SecurityThreshold" => "Face recognition similarity match confidence percentage",
                        "EmailNotifyAbsences" => "Enable automatic warnings emails to parent/user on absences",
                        _ => "System configuration setting"
                    },
                    Group = s.Key switch
                    {
                        "AttendanceStartTime" => "Attendance Rules",
                        "LateGracePeriodMinutes" => "Attendance Rules",
                        "SecurityThreshold" => "AI Recognition Settings",
                        "EmailNotifyAbsences" => "General System",
                        _ => "General System"
                    }
                }).ToList()
            };

            ViewData["Title"] = "System Settings";
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SettingListViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var item in model.Settings)
            {
                var settings = await _unitOfWork.Settings.FindAsync(s => s.Key == item.Key);
                var setting = settings.FirstOrDefault();
                if (setting != null)
                {
                    setting.Value = item.Value;
                    _unitOfWork.Settings.Update(setting);
                }
                else
                {
                    // Create new if missing
                    var newSetting = new Setting
                    {
                        Key = item.Key,
                        Value = item.Value
                    };
                    await _unitOfWork.Settings.AddAsync(newSetting);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            TempData["SuccessMessage"] = "System configuration settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
