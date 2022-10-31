using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class populateTaxType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
    table: "TaxTypes",
    columns: new[] { "BrinksCode", "CWCode", "ActualValue" },
    values: new object[,] {

                { "0VAT", "FREEVAT","0" },
                { "EUROPE0", "FREEVAT","0" },
                { "NONEURO0", "FREEVAT","0" },
                { "VATPERCENT", "VAT","VAT" }

    }
    );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
