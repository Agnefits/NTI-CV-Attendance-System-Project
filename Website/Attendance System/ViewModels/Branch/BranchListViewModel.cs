using System;

namespace Attendance_System.ViewModels.Branch
{
    public class BranchListViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public int StudentCount { get; set; }
    }
}
