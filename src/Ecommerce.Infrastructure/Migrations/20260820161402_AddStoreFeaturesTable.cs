using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreFeaturesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Truck"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreFeatures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreFeatures_DisplayOrder",
                table: "StoreFeatures",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_StoreFeatures_IsActive",
                table: "StoreFeatures",
                column: "IsActive");

            migrationBuilder.InsertData(
                table: "StoreFeatures",
                columns: new[] { "Id", "Title", "Description", "IconName", "DisplayOrder", "IsActive", "CreatedAt" },
                values: new object[,]
                {
                    { Guid.Parse("11111111-1111-1111-1111-111111111111"), "الشحن مجاني", "للطلبات فوق ₪50. توصيل سريع حتى باب منزلك.", "Truck", 1, true, DateTime.UtcNow },
                    { Guid.Parse("22222222-2222-2222-2222-222222222222"), "دفع آمن", "إتمام شراء آمن 100% مع خيارات دفع متعددة.", "Shield", 2, true, DateTime.UtcNow },
                    { Guid.Parse("33333333-3333-3333-3333-333333333333"), "إرجاع سهل", "سياسة إرجاع بدون عناء خلال 30 يوماً على جميع المنتجات.", "RotateCcw", 3, true, DateTime.UtcNow },
                    { Guid.Parse("44444444-4444-4444-4444-444444444444"), "ضمان الجودة", "منتجات ممتازة مختارة بعناية من أجلك.", "Package", 4, true, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreFeatures");
        }
    }
}
