using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyIncomeToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyIncome",
                table: "Users",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyIncome",
                table: "Users");
        }
    }
}
