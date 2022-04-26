using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class populateDataServiceLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table:"serviceLevels",
                columns:new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "D2D", "DD", "Door to Door"}
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "AF", "AF", "Air Freight" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "AD", "AD", "Airport-To-Door" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "DA", "DA", "Door-To-Airport" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "AS", "AS", "Additional Service" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "BU", "BU", "Door-To-UPS" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "UU", "UU", "UPS-To-UPS" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "BF", "BF", "Door-To-FedEx" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "FF", "FF", "FedEx-To-FedEx" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "BP", "BP", "Brink’s-To-Purolator" }
                );
            migrationBuilder.InsertData(
                table: "serviceLevels",
                columns: new[] { "CWCode", "BrinksCode", "Description" },
                values: new object[] { "PS", "PS", "Purolator-To-Purolat" }
                );


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
