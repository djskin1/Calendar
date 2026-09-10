using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calendar.Migrations
{
    /// <inheritdoc />
    public partial class CompleteStatusTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayNameResourceKey",
                table: "CalendarStatuses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 1,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 2,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 3,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 4,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 5,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 6,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 7,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 8,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 9,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 10,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 11,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 12,
                column: "DisplayNameResourceKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "CalendarStatuses",
                keyColumn: "Id",
                keyValue: 13,
                column: "DisplayNameResourceKey",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayNameResourceKey",
                table: "CalendarStatuses");
        }
    }
}
