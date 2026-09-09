using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Calendar.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ForegroundColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSelectable = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemStatus = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarStatuses", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CalendarStatuses",
                columns: new[] { "Id", "BackgroundColor", "Code", "CreatedAt", "Description", "DisplayName", "ForegroundColor", "IsActive", "IsSelectable", "IsSystemStatus", "ModifiedAt", "SortOrder" },
                values: new object[,]
                {
                    { 1, "#D1FAE5", "OFFI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Working at the office.", "Office", "#065F46", true, true, false, null, 10 },
                    { 2, "#DBEAFE", "HOME", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Working from home.", "Home", "#1E40AF", true, true, false, null, 20 },
                    { 3, "#EDE9FE", "HO/AB", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Home / Absent", "#5B21B6", true, true, false, null, 30 },
                    { 4, "#FEE2E2", "OF/AB", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Office / Absent", "#991B1B", true, true, false, null, 40 },
                    { 5, "#E0F2FE", "HO/OF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Home / Office", "#075985", true, true, false, null, 50 },
                    { 6, "#FEF3C7", "CONG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Congress", "#92400E", true, true, false, null, 60 },
                    { 7, "#CFFAFE", "MEET", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Meeting", "#155E75", true, true, false, null, 70 },
                    { 8, "#FCE7F3", "MV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Leave", "#9D174D", true, true, false, null, 80 },
                    { 9, "#F3F4F6", "OPT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Optional", "#374151", true, true, false, null, 90 },
                    { 10, "#FECACA", "ABS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Absent", "#7F1D1D", true, true, false, null, 100 },
                    { 11, "#D1D5DB", "Weekend", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Weekend", "#4B5563", true, false, true, null, 110 },
                    { 12, "#FDE68A", "PUB", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Public holiday", "#78350F", true, false, true, null, 120 },
                    { 13, "#DDD6FE", "MF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Medical / Family", "#4C1D95", true, true, false, null, 130 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarStatuses_Code",
                table: "CalendarStatuses",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarStatuses");
        }
    }
}
