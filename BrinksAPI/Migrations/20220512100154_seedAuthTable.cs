using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class seedAuthTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
    table: "AuthenticationLevels",
    columns: new[] { "AuthId", "AuthName" },
    values: new object[] { 1, "Admin",  }
    );
            migrationBuilder.InsertData(
table: "AuthenticationLevels",
columns: new[] { "AuthId", "AuthName" },
values: new object[] { 2, "User", }
);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
