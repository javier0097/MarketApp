using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeMovementDateADate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stored values are UTC instants; the day must be the one in the store's time zone,
            // which is the time zone of the laptop applying the migration
            migrationBuilder.Sql("""UPDATE "InventoryMovements" SET "MovementDate" = date("MovementDate", 'localtime');""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The time of day is lost; each date becomes its local midnight
            migrationBuilder.Sql("""UPDATE "InventoryMovements" SET "MovementDate" = datetime("MovementDate", 'utc');""");
        }
    }
}
