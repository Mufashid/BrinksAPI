using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class seedUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
table: "users",
columns: new[] { "FirstName", "LastName", "Email", "Password", "isActive", "CreatedTime", "UpdatedTime", "AuthLevelRefId" },
values: new object[] { "admin", "admin", "admin@brinks.com", "P@$$w0rd", true,DateTime.Now,DateTime.Now,1 }
);
            migrationBuilder.InsertData(
table: "users",
columns: new[] { "FirstName", "LastName", "Email", "Password", "isActive", "CreatedTime", "UpdatedTime", "AuthLevelRefId" },
values: new object[] { "user", "user", "user@brinks.com", "P@$$w0rd", true, DateTime.Now, DateTime.Now, 2 }
);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
