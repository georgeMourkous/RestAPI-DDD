using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestApiDdd.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260509190459_SeedStaticPackageCategories")]
    public partial class SeedStaticPackageCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [PackageCategory] WHERE [Id] = 2)
                BEGIN
                    UPDATE [PackageCategory]
                    SET [Name] = N'Share Plan',
                        [SortOrder] = 1,
                        [Visible] = CAST(0 AS bit)
                    WHERE [Id] = 1;

                    SET IDENTITY_INSERT [PackageCategory] ON;

                    INSERT INTO [PackageCategory] ([Id], [Name], [SortOrder], [Visible])
                    VALUES
                        (2, N'Default', 2, CAST(1 AS bit)),
                        (3, N'Billing Activation', 3, CAST(1 AS bit)),
                        (4, N'Share Plan Add-on', 4, CAST(1 AS bit)),
                        (5, N'One Time Billing', 5, CAST(1 AS bit)),
                        (6, N'Global Add-on', 6, CAST(0 AS bit));

                    SET IDENTITY_INSERT [PackageCategory] OFF;

                UPDATE [Package]
                SET [PackageCategoryId] = 2
                WHERE [PackageCategoryId] = 1;

                END;

                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Package]
                SET [PackageCategoryId] = 1
                WHERE [PackageCategoryId] IN (2, 3, 4, 5, 6);

                DELETE FROM [PackageCategory]
                WHERE [Id] IN (2, 3, 4, 5, 6);

                UPDATE [PackageCategory]
                SET [Name] = N'Default',
                    [SortOrder] = 1,
                    [Visible] = CAST(1 AS bit)
                WHERE [Id] = 1;
                """);
        }
    }
}
