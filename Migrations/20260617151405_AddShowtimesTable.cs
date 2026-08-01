using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieCinema.Migrations
{
    /// <inheritdoc />
    public partial class AddShowtimesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdersItems_Movies_MovieId",
                table: "OrdersItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCartItems_Movies_MovieId",
                table: "ShoppingCartItems");

            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "ShoppingCartItems",
                newName: "ShowtimeId");

            migrationBuilder.RenameIndex(
                name: "IX_ShoppingCartItems_MovieId",
                table: "ShoppingCartItems",
                newName: "IX_ShoppingCartItems_ShowtimeId");

            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "OrdersItems",
                newName: "ShowtimeId");

            migrationBuilder.RenameIndex(
                name: "IX_OrdersItems_MovieId",
                table: "OrdersItems",
                newName: "IX_OrdersItems_ShowtimeId");

            migrationBuilder.CreateTable(
                name: "Showtimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false),
                    MovieId = table.Column<int>(type: "int", nullable: false),
                    CinemaRoomId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showtimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Showtimes_CinemaRooms_CinemaRoomId",
                        column: x => x.CinemaRoomId,
                        principalTable: "CinemaRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Showtimes_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_CinemaRoomId",
                table: "Showtimes",
                column: "CinemaRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_MovieId",
                table: "Showtimes",
                column: "MovieId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdersItems_Showtimes_ShowtimeId",
                table: "OrdersItems",
                column: "ShowtimeId",
                principalTable: "Showtimes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCartItems_Showtimes_ShowtimeId",
                table: "ShoppingCartItems",
                column: "ShowtimeId",
                principalTable: "Showtimes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdersItems_Showtimes_ShowtimeId",
                table: "OrdersItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCartItems_Showtimes_ShowtimeId",
                table: "ShoppingCartItems");

            migrationBuilder.DropTable(
                name: "Showtimes");

            migrationBuilder.RenameColumn(
                name: "ShowtimeId",
                table: "ShoppingCartItems",
                newName: "MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_ShoppingCartItems_ShowtimeId",
                table: "ShoppingCartItems",
                newName: "IX_ShoppingCartItems_MovieId");

            migrationBuilder.RenameColumn(
                name: "ShowtimeId",
                table: "OrdersItems",
                newName: "MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_OrdersItems_ShowtimeId",
                table: "OrdersItems",
                newName: "IX_OrdersItems_MovieId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdersItems_Movies_MovieId",
                table: "OrdersItems",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCartItems_Movies_MovieId",
                table: "ShoppingCartItems",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
