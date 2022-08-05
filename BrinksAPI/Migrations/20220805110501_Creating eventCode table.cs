using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class CreatingeventCodetable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrinksCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CWCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventCodes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventCodes");
        }
    }
}
