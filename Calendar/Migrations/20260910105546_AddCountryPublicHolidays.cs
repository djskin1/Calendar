using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calendar.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryPublicHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PublicHolidays",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "PublicHolidays",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsNationalHoliday",
                table: "PublicHolidays",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfficialPublicHoliday",
                table: "PublicHolidays",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "PublicHolidays",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "PublicHolidays",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicHolidayCountries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CountryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    IsBlockingCountry = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedYear = table.Column<int>(type: "int", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidayCountries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_CountryCode_Date_Name",
                table: "PublicHolidays",
                columns: new[] { "CountryCode", "Date", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidayCountries_CountryCode",
                table: "PublicHolidayCountries",
                column: "CountryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidayCountries_IsBlockingCountry",
                table: "PublicHolidayCountries",
                column: "IsBlockingCountry",
                unique: true,
                filter: "[IsBlockingCountry] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicHolidayCountries");

            migrationBuilder.DropIndex(
                name: "IX_PublicHolidays_CountryCode_Date_Name",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "IsNationalHoliday",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "IsOfficialPublicHoliday",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "PublicHolidays");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "PublicHolidays");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PublicHolidays",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
