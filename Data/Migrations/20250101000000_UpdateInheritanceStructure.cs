using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInheritanceStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add Address column to Parents table first
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Parents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Step 2: Add Id column to Parents table
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Parents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())");

            // Step 3: Add all UserAccount properties to Parents
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Parents",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Parents",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "HashedPassword",
                table: "Parents",
                type: "varbinary(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Parents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Parents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Parents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Parents",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Parents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Step 4: Add Id column to Drivers table
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Drivers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())");

            // Step 5: Add all UserAccount properties to Drivers
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Drivers",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Drivers",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "HashedPassword",
                table: "Drivers",
                type: "varbinary(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Drivers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Drivers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Drivers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Drivers",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Drivers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Step 6: Add Id column to Admins table
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Admins",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())");

            // Step 7: Add all UserAccount properties to Admins
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Admins",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Admins",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "HashedPassword",
                table: "Admins",
                type: "varbinary(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Admins",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Admins",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Admins",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Admins",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Admins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Step 8: Set primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_Parents",
                table: "Parents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Drivers",
                table: "Drivers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Admins",
                table: "Admins",
                column: "Id");

            // Step 9: Add indexes
            migrationBuilder.CreateIndex(
                name: "IX_Parents_PhoneNumber",
                table: "Parents",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "UQ_Parents_Email",
                table: "Parents",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_PhoneNumber",
                table: "Drivers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "UQ_Drivers_Email",
                table: "Drivers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_PhoneNumber",
                table: "Admins",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "UQ_Admins_Email",
                table: "Admins",
                column: "Email",
                unique: true);

            // Step 10: Now we can safely drop the old UserAccountId columns
            migrationBuilder.DropColumn(
                name: "UserAccountId",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "UserAccountId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "UserAccountId",
                table: "Admins");

            // Step 11: Drop old foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "FK_Admins_UserAccounts_UserAccountId",
                table: "Admins");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_UserAccounts_UserAccountId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Parents_UserAccounts_UserAccountId",
                table: "Parents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This is a complex migration, rolling back would require significant data migration
            // For now, we'll throw an exception to prevent accidental rollback
            throw new NotSupportedException("Rolling back this migration is not supported due to data structure changes.");
        }
    }
}

