using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestimentoIdToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvestimentoId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InvestimentoId",
                table: "Transactions",
                column: "InvestimentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Investimentos_InvestimentoId",
                table: "Transactions",
                column: "InvestimentoId",
                principalTable: "Investimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Investimentos_InvestimentoId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_InvestimentoId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InvestimentoId",
                table: "Transactions");
        }
    }
}
