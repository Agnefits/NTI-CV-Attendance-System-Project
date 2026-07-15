using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Attendance_System.Data;
using Attendance_System.Helpers;
using Attendance_System.Models.Entities;
using Attendance_System.Models.Enums;

namespace Attendance_System.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 1. Seed Branches
            Branch? mainBranch = null;
            if (!await context.Branches.AnyAsync())
            {
                mainBranch = new Branch
                {
                    Name = "Main Campus",
                    Location = "Building A, Suite 100"
                };
                var eastBranch = new Branch
                {
                    Name = "East Campus",
                    Location = "Building B, Suite 200"
                };

                await context.Branches.AddRangeAsync(mainBranch, eastBranch);
                await context.SaveChangesAsync();
            }
            else
            {
                mainBranch = await context.Branches.FirstOrDefaultAsync();
            }

            // 2. Seed Levels
            Level? level1 = null;
            if (!await context.Levels.AnyAsync())
            {
                level1 = new Level { Title = "Grade 10" };
                var level2 = new Level { Title = "Grade 11" };
                var level3 = new Level { Title = "Grade 12" };

                await context.Levels.AddRangeAsync(level1, level2, level3);
                await context.SaveChangesAsync();
            }
            else
            {
                level1 = await context.Levels.FirstOrDefaultAsync();
            }

            // 3. Seed Classes
            Class? classA = null;
            if (!await context.Classes.AnyAsync() && level1 != null)
            {
                classA = new Class { Title = "Class A", LevelId = level1.Id };
                var classB = new Class { Title = "Class B", LevelId = level1.Id };

                await context.Classes.AddRangeAsync(classA, classB);
                await context.SaveChangesAsync();
            }
            else
            {
                classA = await context.Classes.FirstOrDefaultAsync();
            }

            // 4. Seed ClassRooms
            ClassRoom? room101 = null;
            if (!await context.ClassRooms.AnyAsync())
            {
                room101 = new ClassRoom
                {
                    Title = "Room 101",
                    Location = "First Floor, West Wing",
                    Notes = "Equipped with AI cameras"
                };
                var room102 = new ClassRoom
                {
                    Title = "Room 102",
                    Location = "First Floor, East Wing",
                    Notes = "Computer Lab"
                };

                await context.ClassRooms.AddRangeAsync(room101, room102);
                await context.SaveChangesAsync();
            }
            else
            {
                room101 = await context.ClassRooms.FirstOrDefaultAsync();
            }

            // 5. Seed Admin User
            if (!await context.AdminUsers.AnyAsync())
            {
                var admin = new AdminUser
                {
                    Username = "admin",
                    Password = PasswordHelper.HashPassword("Admin@123"),
                    Email = "admin@attendancesystem.com",
                    PhoneNumber = "1234567890",
                    Fullname = "System Administrator",
                    BranchId = mainBranch?.Id
                };

                await context.AdminUsers.AddAsync(admin);
                await context.SaveChangesAsync();
            }

            // 6. Seed Employee (Teacher)
            if (!await context.Employees.AnyAsync())
            {
                var teacher = new Employee
                {
                    Username = "teacher",
                    Password = PasswordHelper.HashPassword("Teacher@123"),
                    Email = "teacher@attendancesystem.com",
                    PhoneNumber = "0987654321",
                    Fullname = "John Doe",
                    JobTitle = "Teacher",
                    Speciality = "Mathematics",
                    BranchId = mainBranch?.Id
                };
                // Make sure the role is explicitly set to Roles.Teacher as required
                teacher.Role = Roles.Teacher;

                await context.Employees.AddAsync(teacher);
                await context.SaveChangesAsync();
            }

            // 7. Seed Student
            if (!await context.Students.AnyAsync() && level1 != null)
            {
                var student = new Student
                {
                    Username = "student",
                    Password = PasswordHelper.HashPassword("Student@123"),
                    Email = "student@attendancesystem.com",
                    PhoneNumber = "5551234567",
                    Fullname = "Alice Smith",
                    LevelId = level1.Id,
                    ClassId = classA?.Id
                };

                await context.Students.AddAsync(student);
                await context.SaveChangesAsync();
            }

            // 8. Seed Settings
            if (!await context.Settings.AnyAsync())
            {
                var settings = new List<Setting>
                {
                    // --- General ---
                    new Setting { Key = "MinAttendancePercentage",    Value = "75" },
                    new Setting { Key = "SystemName",                 Value = "AI Attendance System" },
                    new Setting { Key = "AllowLateArrivals",          Value = "True" },

                    // --- Attendance Rules ---
                    new Setting { Key = "AttendanceStartTime",        Value = "08:00" },
                    new Setting { Key = "LateGracePeriodMinutes",     Value = "15" },
                    new Setting { Key = "EmailNotifyAbsences",        Value = "False" },

                    // --- Employee Check-In / Check-Out Windows ---
                    new Setting { Key = "EmployeeCheckInStart",       Value = "07:30" },
                    new Setting { Key = "EmployeeCheckInEnd",         Value = "09:30" },
                    new Setting { Key = "EmployeeCheckOutStart",      Value = "15:00" },
                    new Setting { Key = "EmployeeCheckOutEnd",        Value = "19:00" },

                    // --- AI / Face Recognition ---
                    new Setting { Key = "AIModelVersion",             Value = "CVFaceRecoV1" },
                    new Setting { Key = "AIModelSecretKey",           Value = "change-me-in-production-key-1234" },
                    new Setting { Key = "AIServiceBaseUrl",           Value = "http://localhost:8000" },
                    new Setting { Key = "SecurityThreshold",          Value = "0.40" },
                    new Setting { Key = "MinEmbeddingQuality",        Value = "0.60" },
                    new Setting { Key = "LivenessDetectionEnabled",   Value = "False" },
                };

                await context.Settings.AddRangeAsync(settings);
                await context.SaveChangesAsync();
            }
        }
    }
}
