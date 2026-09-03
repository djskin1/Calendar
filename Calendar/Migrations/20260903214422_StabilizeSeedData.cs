using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calendar.Migrations
{
    /// <inheritdoc />
    public partial class StabilizeSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "ApplicationBranding",
                newName: "Id");

            migrationBuilder.InsertData(
                table: "ApplicationBranding",
                columns: new[] { "Id", "CompanyName", "LogoContentType", "LogoData", "LogoFileName", "ModifiedAt" },
                values: new object[] { 1, "Central calendar", null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "SystemInformation",
                columns: new[] { "Id", "LatestClientVersion", "MinimumClientVersion", "ModifiedAt" },
                values: new object[] { 1, "2.0.0", "2.0.0", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApplicationBranding",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SystemInformation",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ApplicationBranding",
                newName: "id");
        }
    }
}
