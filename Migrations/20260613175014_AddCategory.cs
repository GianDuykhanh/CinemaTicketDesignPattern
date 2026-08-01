using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieCinema.Migrations
{
    /// <inheritdoc />
    public partial class AddCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MovieCategory",
                table: "Movies",
                newName: "CategoryId");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.Sql("SET IDENTITY_INSERT [Categories] ON;");
            migrationBuilder.Sql("INSERT INTO [Categories] ([Id], [Name], [Description]) VALUES (1, 'Action', 'Action movies filled with suspense and excitement');");
            migrationBuilder.Sql("INSERT INTO [Categories] ([Id], [Name], [Description]) VALUES (2, 'Comedy', 'Comedy movies that make you laugh');");
            migrationBuilder.Sql("INSERT INTO [Categories] ([Id], [Name], [Description]) VALUES (3, 'Drama', 'Drama movies with deep storyline and characters');");
            migrationBuilder.Sql("INSERT INTO [Categories] ([Id], [Name], [Description]) VALUES (4, 'Documentary', 'Informative and educational documentaries');");
            migrationBuilder.Sql("INSERT INTO [Categories] ([Id], [Name], [Description]) VALUES (5, 'Horror', 'Scary and thrilling horror movies');");
            migrationBuilder.Sql("INSERT INTO [Categories] ([Id], [Name], [Description]) VALUES (6, 'Cartoon', 'Cartoon and animation movies for all ages');");
            migrationBuilder.Sql("SET IDENTITY_INSERT [Categories] OFF;");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_CategoryId",
                table: "Movies",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_Categories_CategoryId",
                table: "Movies",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movies_Categories_CategoryId",
                table: "Movies");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Movies_CategoryId",
                table: "Movies");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Movies",
                newName: "MovieCategory");
        }
    }
}
