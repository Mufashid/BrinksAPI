using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrinksAPI.Migrations
{
    public partial class CreateDocumentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CWDocumentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentTypeCode = table.Column<int>(type: "int", maxLength: 5, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentReference = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    DocumentReferenceId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    DocumentFormat = table.Column<int>(type: "int", maxLength: 4, nullable: false),
                    DocumentDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
