using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestApiDdd.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260511152521_AddServiceStatusType")]
    public partial class AddServiceStatusType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceStatusType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TokenName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStatusType", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                SET IDENTITY_INSERT [ServiceStatusType] ON;

                INSERT INTO [ServiceStatusType] ([Id], [Name], [SortOrder], [TokenName])
                VALUES
                    (1, N'Recurring Charge', 1, N'mrc'),
                    (2, N'Non-recurring Charge', 2, N'nrc'),
                    (3, N'Fee', 3, N'fee');

                SET IDENTITY_INSERT [ServiceStatusType] OFF;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatusType_Name",
                table: "ServiceStatusType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStatusType_TokenName",
                table: "ServiceStatusType",
                column: "TokenName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceStatusType");
        }
    }
}
