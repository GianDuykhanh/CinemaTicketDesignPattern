using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieCinema.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatsToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedSeats",
                table: "OrdersItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedSeats",
                table: "OrdersItems");
        }
    }
}
