using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinancialGoalId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FinancialGoalId",
                table: "Transactions",
                column: "FinancialGoalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FinancialGoals_FinancialGoalId",
                table: "Transactions",
                column: "FinancialGoalId",
                principalTable: "FinancialGoals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FinancialGoals_FinancialGoalId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FinancialGoalId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FinancialGoalId",
                table: "Transactions");
        }
    }
}
