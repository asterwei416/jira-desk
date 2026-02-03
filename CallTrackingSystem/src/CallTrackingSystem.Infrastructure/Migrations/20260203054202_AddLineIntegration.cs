using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallTrackingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLineIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LineBoundAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineDisplayName",
                table: "Users",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineBoundAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LineDisplayName",
                table: "Users");
        }
    }
}
