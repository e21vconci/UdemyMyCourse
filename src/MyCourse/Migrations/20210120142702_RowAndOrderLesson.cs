using Microsoft.EntityFrameworkCore.Migrations;

namespace MyCourse.Migrations
{
    public partial class RowAndOrderLesson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Lessons",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<string>(
                name: "RowVersion",
                table: "Lessons",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Lessons");
        }
    }
}
