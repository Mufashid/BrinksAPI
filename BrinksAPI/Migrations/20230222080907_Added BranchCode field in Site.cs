using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class AddedBranchCodefieldinSite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                table: "sites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentFormat",
                table: "documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "sites");

            migrationBuilder.AlterColumn<int>(
                name: "DocumentFormat",
                table: "documents",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
