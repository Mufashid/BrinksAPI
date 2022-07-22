using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class Newfieldaddedsites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "sites",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "sites");
        }
    }
}
