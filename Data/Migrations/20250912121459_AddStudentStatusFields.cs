using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "Students",
                type: "datetime2(3)",
                precision: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "Students",
                type: "datetime2(3)",
                precision: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "Students",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Students",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440010"),
                columns: new[] { "ActivatedAt", "DeactivatedAt", "DeactivationReason" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Students",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440011"),
                columns: new[] { "ActivatedAt", "DeactivatedAt", "DeactivationReason" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Students",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440012"),
                columns: new[] { "ActivatedAt", "DeactivatedAt", "DeactivationReason" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "Students");
        }
    }
}
