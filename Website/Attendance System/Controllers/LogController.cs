using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Attendance_System.Attributes;
using Attendance_System.Models.Enums;
using Attendance_System.UnitOfWork.Interfaces;
using Attendance_System.ViewModels.Log;

namespace Attendance_System.Controllers
{
    [AuthorizedRoles(Roles.Admin)]
    public class LogController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index(LogFilterViewModel filter)
        {
            var logs = await _unitOfWork.Logs.GetAllAsync();
            var users = await _unitOfWork.BaseUsers.GetAllAsync();

            if (filter.UserId.HasValue)
            {
                logs = logs.Where(l => l.ByUser == filter.UserId.Value);
            }
            if (!string.IsNullOrEmpty(filter.Action))
            {
                logs = logs.Where(l => l.Action.Equals(filter.Action, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(filter.ItemTable))
            {
                logs = logs.Where(l => l.ItemTable.Equals(filter.ItemTable, StringComparison.OrdinalIgnoreCase));
            }
            if (filter.FromDate.HasValue)
            {
                logs = logs.Where(l => l.CreatedAt >= filter.FromDate.Value);
            }
            if (filter.ToDate.HasValue)
            {
                logs = logs.Where(l => l.CreatedAt <= filter.ToDate.Value);
            }

            filter.Logs = logs.Select(l => new LogListViewModel
            {
                Id = l.Id,
                ByUserName = users.FirstOrDefault(u => u.Id == l.ByUser)?.Username ?? "System",
                Action = l.Action,
                ItemTable = l.ItemTable,
                ItemId = l.ItemId,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            }).OrderByDescending(l => l.CreatedAt).ToList();

            await PopulateFilterOptions(filter);

            ViewData["Title"] = "Audit Logs";
            return View(filter);
        }

        private async Task PopulateFilterOptions(LogFilterViewModel model)
        {
            var users = await _unitOfWork.BaseUsers.GetAllAsync();
            var logs = await _unitOfWork.Logs.GetAllAsync();

            model.UserOptions = users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.Username
            }).ToList();

            model.ActionOptions = logs.Select(l => l.Action).Distinct().Select(a => new SelectListItem
            {
                Value = a,
                Text = a
            }).ToList();

            model.TableOptions = logs.Select(l => l.ItemTable).Distinct().Select(t => new SelectListItem
            {
                Value = t,
                Text = t
            }).ToList();
        }
    }
}
