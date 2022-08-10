using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class populateEventCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
               table: "eventCodes",
               columns: new[] { "CWCode", "BrinksCode" },
               values: new object[,] {

                {"FMA","FMA"},
{"FNA","FNA"},
{"Z10","MNT"},
{"Z13","MFA"},
{"Z12","MNA"},
{"Z11","MFT"},
{"EMS","EMA"},
{"CIN","CIN"},
{"ICC","ICC"},
{"Z14","IV"},
{"Z15","OV"},
{"Z95","STA"},
{"TCM","TFM"},

               }
               );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
