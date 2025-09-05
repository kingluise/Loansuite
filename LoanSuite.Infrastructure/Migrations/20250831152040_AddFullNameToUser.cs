using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanSuite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullNameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "FullName" },
                values: new object[] { new DateTime(2025, 8, 31, 15, 20, 39, 440, DateTimeKind.Utc).AddTicks(5169), "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 30, 10, 38, 30, 230, DateTimeKind.Utc).AddTicks(6772));
        }
    }
}
