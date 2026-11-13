using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameMovementRecordedToCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecordedById",
                table: "InventoryMovements",
                newName: "CreatedById");

            migrationBuilder.RenameColumn(
                name: "RecordedAt",
                table: "InventoryMovements",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "InventoryMovements",
                newName: "RecordedById");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "InventoryMovements",
                newName: "RecordedAt");
        }
    }
}
