using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PickupPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Geog = table.Column<Point>(type: "geography", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    HashedPassword = table.Column<byte[]>(type: "varbinary(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    UserPhotoFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_UserAccounts_Id",
                        column: x => x.Id,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    HealthCertificateFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastActiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drivers_UserAccounts_Id",
                        column: x => x.Id,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parents_UserAccounts_Id",
                        column: x => x.Id,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnitPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    ScheduleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartureTime = table.Column<TimeOnly>(type: "time(0)", precision: 0, nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    AdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitPrices_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    HashedLicensePlate = table.Column<byte[]>(type: "varbinary(256)", maxLength: 256, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DriverLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashedLicenseNumber = table.Column<byte[]>(type: "varbinary(256)", maxLength: 256, nullable: false),
                    DateOfIssue = table.Column<DateTime>(type: "date", nullable: false),
                    IssuedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LicenseImageFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverLicenses_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverWorkingHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverWorkingHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverWorkingHours_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CurrentPickupPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PickupPointAssignedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Parents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Students_PickupPoints_CurrentPickupPointId",
                        column: x => x.CurrentPickupPointId,
                        principalTable: "PickupPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Parents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DriverLeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoReplacementEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SuggestedReplacementDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedReplacementVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestionGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverLeaveRequests_Admins_ApprovedByAdminId",
                        column: x => x.ApprovedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DriverLeaveRequests_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverLeaveRequests_Drivers_SuggestedReplacementDriverId",
                        column: x => x.SuggestedReplacementDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverLeaveRequests_Vehicles_SuggestedReplacementVehicleId",
                        column: x => x.SuggestedReplacementVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimaryDriver = table.Column<bool>(type: "bit", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverVehicles_Admins_ApprovedByAdminId",
                        column: x => x.ApprovedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DriverVehicles_Admins_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverVehicles_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverVehicles_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    HashedURL = table.Column<byte[]>(type: "varbinary(256)", maxLength: 256, nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentGradeEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGradeEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGradeEnrollments_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentGradeEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentPickupPointHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickupPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPickupPointHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPickupPointHistory_PickupPoints_PickupPointId",
                        column: x => x.PickupPointId,
                        principalTable: "PickupPoints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentPickupPointHistory_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportFeeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportFeeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportFeeItems_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransportFeeItems_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DriverLeaveConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TripEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedStudents = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    SuggestedDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacementScore = table.Column<double>(type: "float", nullable: false),
                    ReplacementReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLeaveConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverLeaveConflicts_DriverLeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "DriverLeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverLeaveConflicts_Drivers_SuggestedDriverId",
                        column: x => x.SuggestedDriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DriverLeaveConflicts_Vehicles_SuggestedVehicleId",
                        column: x => x.SuggestedVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "UserAccounts",
                columns: new[] { "Id", "Address", "CreatedAt", "DateOfBirth", "Email", "FirstName", "Gender", "HashedPassword", "LastName", "LockReason", "LockedAt", "LockedBy", "LockedUntil", "PhoneNumber", "UpdatedAt", "UserPhotoFileId" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655440001"), "123 Lê Duẩn, Quận Hải Châu, Đà Nẵng, Vietnam", new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@edubus.com", "Nguyen", 1, new byte[] { 36, 50, 97, 36, 49, 48, 36, 57, 50, 73, 88, 85, 78, 112, 107, 106, 79, 48, 114, 79, 81, 53, 98, 121, 77, 105, 46, 89, 101, 52, 111, 75, 111, 69, 97, 51, 82, 111, 57, 108, 108, 67, 47, 46, 111, 103, 47, 97, 116, 50, 46, 117, 104, 101, 87, 71, 47, 105, 103, 105 }, "Van Admin", null, null, null, null, "0901234567", new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440002"), "456 Trần Phú, Quận Hải Châu, Đà Nẵng, Vietnam", new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(1985, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "driver@edubus.com", "Tran", 1, new byte[] { 36, 50, 97, 36, 49, 48, 36, 57, 50, 73, 88, 85, 78, 112, 107, 106, 79, 48, 114, 79, 81, 53, 98, 121, 77, 105, 46, 89, 101, 52, 111, 75, 111, 69, 97, 51, 82, 111, 57, 108, 108, 67, 47, 46, 111, 103, 47, 97, 116, 50, 46, 117, 104, 101, 87, 71, 47, 105, 103, 105 }, "Van Driver", null, null, null, null, "0901234568", new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440003"), "123 Nguyen Van Linh, District 7, Ho Chi Minh City", new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(1984, 6, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "parent@edubus.com", "Le", 2, new byte[] { 36, 50, 97, 36, 49, 48, 36, 57, 50, 73, 88, 85, 78, 112, 107, 106, 79, 48, 114, 79, 81, 53, 98, 121, 77, 105, 46, 89, 101, 52, 111, 75, 111, 69, 97, 51, 82, 111, 57, 108, 108, 67, 47, 46, 111, 103, 47, 97, 116, 50, 46, 117, 104, 101, 87, 71, 47, 105, 103, 105 }, "Thi Parent", null, null, null, null, "0901234569", new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "Admins",
                column: "Id",
                value: new Guid("550e8400-e29b-41d4-a716-446655440001"));

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "HealthCertificateFileId", "LastActiveDate", "Status", "StatusNote" },
                values: new object[] { new Guid("550e8400-e29b-41d4-a716-446655440002"), null, null, 1, null });

            migrationBuilder.InsertData(
                table: "Parents",
                column: "Id",
                value: new Guid("550e8400-e29b-41d4-a716-446655440003"));

            migrationBuilder.InsertData(
                table: "DriverLicenses",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DateOfIssue", "DriverId", "HashedLicenseNumber", "IsDeleted", "IssuedBy", "LicenseImageFileId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("550e8400-e29b-41d4-a716-446655440004"), new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("550e8400-e29b-41d4-a716-446655440001"), new DateTime(2020, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("550e8400-e29b-41d4-a716-446655440002"), new byte[] { 36, 50, 97, 36, 49, 49, 36, 80, 81, 118, 51, 99, 49, 121, 113, 66, 87, 86, 72, 120, 107, 100, 48, 76, 72, 65, 107, 67, 79, 89, 122, 54, 84, 116, 120, 77, 81, 74, 113, 104, 78, 56, 47, 76, 101, 119, 100, 66, 80, 106, 52, 74, 47, 72, 83, 46, 105, 75, 56, 79 }, false, "Cục Đăng kiểm Việt Nam", null, new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "Students",
                columns: new[] { "Id", "CreatedAt", "CurrentPickupPointId", "FirstName", "IsActive", "LastName", "ParentEmail", "ParentId", "PickupPointAssignedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655440010"), new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null, "Nguyen", true, "Van An", "parent@edubus.com", new Guid("550e8400-e29b-41d4-a716-446655440003"), null, new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("550e8400-e29b-41d4-a716-446655440011"), new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null, "Tran", true, "Thi Binh", "parent@edubus.com", new Guid("550e8400-e29b-41d4-a716-446655440003"), null, new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("550e8400-e29b-41d4-a716-446655440012"), new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null, "Le", true, "Van Cuong", "parent@edubus.com", new Guid("550e8400-e29b-41d4-a716-446655440003"), null, new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverLeaveConflicts_LeaveRequestId",
                table: "DriverLeaveConflicts",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLeaveConflicts_SuggestedDriverId",
                table: "DriverLeaveConflicts",
                column: "SuggestedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLeaveConflicts_SuggestedVehicleId",
                table: "DriverLeaveConflicts",
                column: "SuggestedVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLeaveRequests_ApprovedByAdminId",
                table: "DriverLeaveRequests",
                column: "ApprovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLeaveRequests_DriverId",
                table: "DriverLeaveRequests",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLeaveRequests_SuggestedReplacementDriverId",
                table: "DriverLeaveRequests",
                column: "SuggestedReplacementDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLeaveRequests_SuggestedReplacementVehicleId",
                table: "DriverLeaveRequests",
                column: "SuggestedReplacementVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLicenses_DriverId",
                table: "DriverLicenses",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverLicenses_DriverId1",
                table: "DriverLicenses",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "UQ_DriverLicenses_HashedLicenseNumber",
                table: "DriverLicenses",
                column: "HashedLicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicles_ApprovedByAdminId",
                table: "DriverVehicles",
                column: "ApprovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicles_AssignedByAdminId",
                table: "DriverVehicles",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicles_DriverId",
                table: "DriverVehicles",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicles_VehicleId",
                table: "DriverVehicles",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "UQ_DriverVehicles_PrimaryPerVehicle",
                table: "DriverVehicles",
                column: "VehicleId",
                unique: true,
                filter: "([IsPrimaryDriver]=(1) AND [EndTimeUtc] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_DriverWorkingHours_DriverId",
                table: "DriverWorkingHours",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "UQ_Grades_Name",
                table: "Grades",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_StudentId",
                table: "Images",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UQ_Images_HashedURL",
                table: "Images",
                column: "HashedURL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupPoints_Description",
                table: "PickupPoints",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SGE_GradeId",
                table: "StudentGradeEnrollments",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SGE_StudentId",
                table: "StudentGradeEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UQ_SGE_Student_Grade_Start",
                table: "StudentGradeEnrollments",
                columns: new[] { "StudentId", "GradeId", "StartTimeUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPickupPointHistory_AssignedAt",
                table: "StudentPickupPointHistory",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPickupPointHistory_PickupPointId",
                table: "StudentPickupPointHistory",
                column: "PickupPointId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPickupPointHistory_StudentId",
                table: "StudentPickupPointHistory",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CurrentPickupPointId",
                table: "Students",
                column: "CurrentPickupPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ParentEmail",
                table: "Students",
                column: "ParentEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ParentId",
                table: "Students",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ParentId_CreatedAt",
                table: "Transactions",
                columns: new[] { "ParentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_Transactions_TransactionCode",
                table: "Transactions",
                column: "TransactionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportFeeItems_Student_Status",
                table: "TransportFeeItems",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TransportFeeItems_TransactionId",
                table: "TransportFeeItems",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPrices_AdminId",
                table: "UnitPrices",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPrices_ValidFrom",
                table: "UnitPrices",
                column: "StartTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_PhoneNumber",
                table: "UserAccounts",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UserAccounts_Email",
                table: "UserAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_AdminId",
                table: "Vehicles",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "UQ_Vehicles_HashedLicensePlate",
                table: "Vehicles",
                column: "HashedLicensePlate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverLeaveConflicts");

            migrationBuilder.DropTable(
                name: "DriverLicenses");

            migrationBuilder.DropTable(
                name: "DriverVehicles");

            migrationBuilder.DropTable(
                name: "DriverWorkingHours");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "StudentGradeEnrollments");

            migrationBuilder.DropTable(
                name: "StudentPickupPointHistory");

            migrationBuilder.DropTable(
                name: "TransportFeeItems");

            migrationBuilder.DropTable(
                name: "UnitPrices");

            migrationBuilder.DropTable(
                name: "DriverLeaveRequests");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "PickupPoints");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "UserAccounts");
        }
    }
}
