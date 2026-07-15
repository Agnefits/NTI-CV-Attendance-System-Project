using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendance_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceRecognitionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Embedding",
                table: "FaceEmbeddings",
                newName: "EmbeddingJson");

            migrationBuilder.AddColumn<float>(
                name: "RecognitionConfidence",
                table: "StudentAttendances",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptureAngle",
                table: "FaceEmbeddings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "FaceEmbeddings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "QualityScore",
                table: "FaceEmbeddings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceDate",
                table: "EmployeeAttendances",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckInTime",
                table: "EmployeeAttendances",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckOutTime",
                table: "EmployeeAttendances",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "RecognitionConfidence",
                table: "EmployeeAttendances",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecognitionConfidence",
                table: "StudentAttendances");

            migrationBuilder.DropColumn(
                name: "CaptureAngle",
                table: "FaceEmbeddings");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "FaceEmbeddings");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "FaceEmbeddings");

            migrationBuilder.DropColumn(
                name: "AttendanceDate",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "RecognitionConfidence",
                table: "EmployeeAttendances");

            migrationBuilder.RenameColumn(
                name: "EmbeddingJson",
                table: "FaceEmbeddings",
                newName: "Embedding");
        }
    }
}
