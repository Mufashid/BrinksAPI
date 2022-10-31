using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class populateCategoryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
    table: "Categories",
    columns: new[] { "BrinksCode", "BrinksDescription","CWCode", "CWDescrption" },
    values: new object[,] {
        {"OTHER","Other Charges","MISC","Miscellaneous"},
{"VALUE","Value Charges","LIAB","Liability"},
{"INSURANCE","India Shipments","GSLOC","Ground support"},
{"GROUND","Ground support","GSLOC","Ground support"},
{"WEIGHT","Extra Weight","WGHT","Excess weight"},
{"AIRLINE","Airline charge","FRTAIR","Air freight"},
{"CUSTOMS","Customs clearance","CCLR","Customs clearance"},
{"VAULT","Vault charges","VAULT","Vaulting/Storage"},
{"TAX","Taxes","DUTY","Duties & Taxes"},
{"USERFEE","User Fee Charges","SPECIAL","Special - User or other fees"},
{"COLLECT","Collect charges","COLLECT","Charges collect"},
{"CHCHARGE","CH Charge","CHCHARGE"," Clearing House charge"},
{"AIRCHARTER","Air Charter","CHART","Air charter"},
{"SEAFREIGHT","Sea Freight","FRTSEA","Sea freight"},
{"STORAGE","Storage","STOR","Storage"},
{"COURIER","Courier","HANDC","Hand Carry, Meet & Assist"},
{"EXTRADELIV","Extra Delivery","DLV","Delivery"},
{"TRADE","Trade","SPECIAL","Special - User or other fees"},
{"LVP","LVP Program CH charge","LVP","Low Value Parcel"}

    }
    );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
