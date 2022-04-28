using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class seedDocumentTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "documentTypes",
                columns: new[] { "BrinksCode","CWCode"},
                values:new object[,]
                {
                   {"AL","MSC"},
{"BOL","BOE"},
{"BR","BDR"},
{"CI","CIV"},
{"CN","CAT"},
{"COO","COO"},
{"CSI","CUS"},
{"CTS","MSC"},
{"EEI","MSC"},
{"FAGSP","MSC"},
{"FRMD","MSC"},
{"GC","MSC"},
{"IMAGE","MSC"},
{"JTEPA","MSC"},
{"KPC","MSC"},
{"LIC","MSC"},
{"MCEUR","MSC"},
{"OTH","MSC"},
{"PFI","MSC"},
{"PID","MSC"},
{"PL","PKL"}

                }
               );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
