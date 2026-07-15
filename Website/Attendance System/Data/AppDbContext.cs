using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Attendance_System.Models.BaseEntities;
using Attendance_System.Models.Entities;

namespace Attendance_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Users and subclasses
        public DbSet<BaseUser> BaseUsers { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Student> Students { get; set; }

        // Entities
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<StudentAttendance> StudentAttendances { get; set; }
        public DbSet<EmployeeAttendance> EmployeeAttendances { get; set; }
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure TPT Inheritance for BaseUser
            modelBuilder.Entity<BaseUser>().ToTable("BaseUsers");
            modelBuilder.Entity<AdminUser>().ToTable("AdminUsers");
            modelBuilder.Entity<Employee>().ToTable("Employees");
            modelBuilder.Entity<Student>().ToTable("Students");

            // Configure Token relationship
            modelBuilder.Entity<Token>()
                .HasOne(t => t.BaseUser)
                .WithMany()
                .HasForeignKey(t => t.BaseUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AdminUser relationships
            modelBuilder.Entity<AdminUser>()
                .HasOne(a => a.Branch)
                .WithMany()
                .HasForeignKey(a => a.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Employee relationships
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Student relationships
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Level)
                .WithMany()
                .HasForeignKey(s => s.LevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany()
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Class relationships
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Level)
                .WithMany()
                .HasForeignKey(c => c.LevelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Lesson relationships
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Level)
                .WithMany()
                .HasForeignKey(l => l.LevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Class)
                .WithMany()
                .HasForeignKey(l => l.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Teacher)
                .WithMany()
                .HasForeignKey(l => l.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.ClassRoom)
                .WithMany()
                .HasForeignKey(l => l.ClassRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Camera relationships
            modelBuilder.Entity<Camera>()
                .HasOne(c => c.ClassRoom)
                .WithMany()
                .HasForeignKey(c => c.ClassRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure StudentAttendance relationships
            modelBuilder.Entity<StudentAttendance>()
                .HasOne(a => a.Camera)
                .WithMany()
                .HasForeignKey(a => a.CameraId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAttendance>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAttendance>()
                .HasOne(a => a.ModifiedByUser)
                .WithMany()
                .HasForeignKey(a => a.ModifiedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAttendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAttendance>()
                .HasOne(a => a.Lesson)
                .WithMany()
                .HasForeignKey(a => a.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure EmployeeAttendance relationships
            modelBuilder.Entity<EmployeeAttendance>()
                .HasOne(a => a.Camera)
                .WithMany()
                .HasForeignKey(a => a.CameraId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeAttendance>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeAttendance>()
                .HasOne(a => a.ModifiedByUser)
                .WithMany()
                .HasForeignKey(a => a.ModifiedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeAttendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure FaceEmbedding relationships
            modelBuilder.Entity<FaceEmbedding>()
                .HasOne(fe => fe.BaseUser)
                .WithMany()
                .HasForeignKey(fe => fe.BaseUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // EmbeddingJson stores a JSON float[] — needs NVARCHAR(MAX)
            modelBuilder.Entity<FaceEmbedding>()
                .Property(fe => fe.EmbeddingJson)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<FaceEmbedding>()
                .Property(fe => fe.CaptureAngle)
                .HasMaxLength(50);

            modelBuilder.Entity<FaceEmbedding>()
                .Property(fe => fe.Label)
                .HasMaxLength(200);

            // Configure Log relationships
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.ByUser)
                .OnDelete(DeleteBehavior.Restrict);

            // Apply global soft-delete query filter
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType) && entityType.BaseType == null)
                {
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(GetIsDeletedFilter(entityType.ClrType));
                }
            }
        }

        private static LambdaExpression GetIsDeletedFilter(Type type)
        {
            var parameter = Expression.Parameter(type, "e");
            var property = Expression.Property(parameter, nameof(BaseModel.IsDeleted));
            var constant = Expression.Constant(false);
            var body = Expression.Equal(property, constant);
            return Expression.Lambda(body, parameter);
        }

        public override int SaveChanges()
        {
            ApplySoftDeleteAndTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplySoftDeleteAndTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplySoftDeleteAndTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseModel && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));

            foreach (var entry in entries)
            {
                var entity = (BaseModel)entry.Entity;
                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                    entity.LastModified = now;
                    entity.IsDeleted = false;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.LastModified = now;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entity.LastModified = now;
                    entity.IsDeleted = true;
                }
            }
        }
    }
}
