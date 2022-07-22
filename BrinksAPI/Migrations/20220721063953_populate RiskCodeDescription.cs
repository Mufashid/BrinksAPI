using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class populateRiskCodeDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "riskCodeDescriptions",
                columns: new[] { "BrinksCode", "CWCode" },
                values: new object[,] {
                
                { "Good", "CR1" },
                { "Poor", "CR2" },
                { "Cash Only", "CR3" },
                { "Collections", "CR5" },
                { "Black Listed", "CR4" },
                { "Suspend A/C", "CR6" },
                
                }
                );

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
