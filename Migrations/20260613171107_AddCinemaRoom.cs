using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieCinema.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CinemaRoomId",
                table: "Movies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CinemaRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CinemaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CinemaRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CinemaRooms_Cinemas_CinemaId",
                        column: x => x.CinemaId,
                        principalTable: "Cinemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_CinemaRoomId",
                table: "Movies",
                column: "CinemaRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CinemaRooms_CinemaId",
                table: "CinemaRooms",
                column: "CinemaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_CinemaRooms_CinemaRoomId",
                table: "Movies",
                column: "CinemaRoomId",
                principalTable: "CinemaRooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movies_CinemaRooms_CinemaRoomId",
                table: "Movies");

            migrationBuilder.DropTable(
                name: "CinemaRooms");

            migrationBuilder.DropIndex(
                name: "IX_Movies_CinemaRoomId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "CinemaRoomId",
                table: "Movies");
        }
    }
}
