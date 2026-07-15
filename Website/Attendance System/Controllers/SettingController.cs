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

            var keyOrder = new System.Collections.Generic.List<string>
            {
                // General
                "SystemName",
                "MinAttendancePercentage",
                "AllowLateArrivals",

                // Attendance Rules
                "AttendanceStartTime",
                "LateGracePeriodMinutes",
                "EmailNotifyAbsences",

                // Employee Time Windows
                "EmployeeCheckInStart",
                "EmployeeCheckInEnd",
                "EmployeeCheckOutStart",
                "EmployeeCheckOutEnd",

                // AI Recognition Settings
                "AIServiceBaseUrl",
                "AIModelVersion",
                "AIModelSecretKey",
                "SecurityThreshold",
                "MinEmbeddingQuality",
                "LivenessDetectionEnabled"
            };

            var sortedSettings = settings.OrderBy(s => {
                var index = keyOrder.IndexOf(s.Key);
                return index >= 0 ? index : int.MaxValue;
            }).ToList();

            var viewModel = new SettingListViewModel
            {
                Settings = sortedSettings.Select(s => new SettingItemViewModel
                {
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Key switch
                    {
                        // General
                        "MinAttendancePercentage"   => "Minimum attendance percentage required to pass (%)",
                        "SystemName"               => "Display name of the system shown in the UI",
                        "AllowLateArrivals"        => "Whether late arrivals are recorded or rejected",

                        // Attendance Rules
                        "AttendanceStartTime"      => "Daily start hour marking the opening of attendance (e.g. 08:00)",
                        "LateGracePeriodMinutes"   => "Grace period minutes before a student is flagged LATE",
                        "EmailNotifyAbsences"      => "Send automatic warning emails on absences (True/False)",

                        // Employee Check-In / Check-Out Windows
                        "EmployeeCheckInStart"     => "Earliest time employees may check in (e.g. 07:30)",
                        "EmployeeCheckInEnd"       => "Latest time employees may check in (e.g. 09:30)",
                        "EmployeeCheckOutStart"    => "Earliest time employees may check out (e.g. 15:00)",
                        "EmployeeCheckOutEnd"      => "Latest time employees may check out (e.g. 19:00)",

                        // AI Recognition
                        "AIModelVersion"           => "Face recognition model version identifier (e.g. CVFaceRecoV1)",
                        "AIModelSecretKey"         => "Secret API key sent to the AI microservice for authentication",
                        "AIServiceBaseUrl"         => "Base URL of the Python FastAPI face recognition service",
                        "SecurityThreshold"        => "Minimum cosine similarity score to accept a face match (0.0–1.0)",
                        "MinEmbeddingQuality"      => "Minimum quality score for an embedding to be stored (0.0–1.0)",
                        "LivenessDetectionEnabled" => "Enable liveness/anti-spoofing check before recognition (True/False)",

                        _ => "System configuration setting"
                    },
                    Group = s.Key switch
                    {
                        "MinAttendancePercentage"   => "General",
                        "SystemName"               => "General",
                        "AllowLateArrivals"        => "General",

                        "AttendanceStartTime"      => "Attendance Rules",
                        "LateGracePeriodMinutes"   => "Attendance Rules",
                        "EmailNotifyAbsences"      => "Attendance Rules",

                        "EmployeeCheckInStart"     => "Employee Time Windows",
                        "EmployeeCheckInEnd"       => "Employee Time Windows",
                        "EmployeeCheckOutStart"    => "Employee Time Windows",
                        "EmployeeCheckOutEnd"      => "Employee Time Windows",

                        "AIModelVersion"           => "AI Recognition Settings",
                        "AIModelSecretKey"         => "AI Recognition Settings",
                        "AIServiceBaseUrl"         => "AI Recognition Settings",
                        "SecurityThreshold"        => "AI Recognition Settings",
                        "MinEmbeddingQuality"      => "AI Recognition Settings",
                        "LivenessDetectionEnabled" => "AI Recognition Settings",

                        _ => "General"
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

            // Validation for start times being before end times
            var checkInStart = model.Settings.FirstOrDefault(s => s.Key == "EmployeeCheckInStart")?.Value;
            var checkInEnd = model.Settings.FirstOrDefault(s => s.Key == "EmployeeCheckInEnd")?.Value;
            var checkOutStart = model.Settings.FirstOrDefault(s => s.Key == "EmployeeCheckOutStart")?.Value;
            var checkOutEnd = model.Settings.FirstOrDefault(s => s.Key == "EmployeeCheckOutEnd")?.Value;

            if (TimeSpan.TryParse(checkInStart, out var cis) && TimeSpan.TryParse(checkInEnd, out var cie) && cis >= cie)
            {
                TempData["ErrorMessage"] = "Employee Check-in Start Time must be before Check-in End Time.";
                return RedirectToAction(nameof(Index));
            }

            if (TimeSpan.TryParse(checkOutStart, out var cos) && TimeSpan.TryParse(checkOutEnd, out var coe) && cos >= coe)
            {
                TempData["ErrorMessage"] = "Employee Check-out Start Time must be before Check-out End Time.";
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
