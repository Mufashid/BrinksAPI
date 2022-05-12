using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class AddAuthTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthLevelRefId",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuthenticationLevels",
                columns: table => new
                {
                    AuthId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationLevels", x => x.AuthId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_AuthLevelRefId",
                table: "users",
                column: "AuthLevelRefId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_AuthenticationLevels_AuthLevelRefId",
                table: "users",
                column: "AuthLevelRefId",
                principalTable: "AuthenticationLevels",
                principalColumn: "AuthId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_AuthenticationLevels_AuthLevelRefId",
                table: "users");

            migrationBuilder.DropTable(
                name: "AuthenticationLevels");

            migrationBuilder.DropIndex(
                name: "IX_users_AuthLevelRefId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AuthLevelRefId",
                table: "users");
        }
    }
}
