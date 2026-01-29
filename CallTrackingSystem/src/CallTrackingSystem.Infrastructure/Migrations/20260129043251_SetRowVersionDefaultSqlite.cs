using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallTrackingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetRowVersionDefaultSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "CallRecords",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "randomblob(8)",
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "CallRecords",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValueSql: "randomblob(8)");
        }
    }
}
