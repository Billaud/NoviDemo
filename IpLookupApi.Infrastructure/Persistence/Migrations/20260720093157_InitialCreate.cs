using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IpLookupApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    TwoLetterCode = table.Column<string>(type: "char(2)", nullable: false),
                    ThreeLetterCode = table.Column<string>(type: "char(3)", nullable: false),
                    CountryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.TwoLetterCode);
                });

            migrationBuilder.CreateTable(
                name: "JobHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcessedRecords = table.Column<int>(type: "int", nullable: false),
                    UpdatedRecords = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpAddress",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "varchar(15)", nullable: false),
                    CountryTwoLetterCode = table.Column<string>(type: "char(2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpAddress_Countries_CountryTwoLetterCode",
                        column: x => x.CountryTwoLetterCode,
                        principalTable: "Countries",
                        principalColumn: "TwoLetterCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_ThreeLetterCode",
                table: "Countries",
                column: "ThreeLetterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpAddress_Address",
                table: "IpAddress",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpAddress_CountryTwoLetterCode_UpdatedAt",
                table: "IpAddress",
                column: "CountryTwoLetterCode")
                .Annotation("SqlServer:Include", new[] { "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpAddress");

            migrationBuilder.DropTable(
                name: "JobHistory");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
