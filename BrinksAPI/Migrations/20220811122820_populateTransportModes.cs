using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class populateTransportModes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
               table: "transportModes",
               columns: new[] { "CWCode", "BrinksCode" },
               values: new object[,] {

                { "AIR", "A" },
                { "SEA", "S" },
                { "ROA", "R" },
                { "RAI", "L" },
                { "AIR", "N" },
                { "COU", "P" },
                { "AIR", "T" },

               }
               );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
