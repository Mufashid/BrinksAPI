using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class populateActionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "actionTypes",
                columns: new[] { "BrinksCode", "CWCode" },
                values: new object[] { "pick", "Picked up date" }
                );
            migrationBuilder.InsertData(
                table: "actionTypes",
                columns: new[] { "BrinksCode", "CWCode" },
                values: new object[] { "dlvd", "Delivery Date" }
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
