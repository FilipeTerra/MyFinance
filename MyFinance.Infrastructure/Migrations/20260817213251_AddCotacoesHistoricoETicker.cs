using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCotacoesHistoricoETicker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "Investimentos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CotacoesHistorico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvestimentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateTime>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotacoesHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotacoesHistorico_Investimentos_InvestimentoId",
                        column: x => x.InvestimentoId,
                        principalTable: "Investimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CotacoesHistorico_InvestimentoId_Data",
                table: "CotacoesHistorico",
                columns: new[] { "InvestimentoId", "Data" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CotacoesHistorico");

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "Investimentos");
        }
    }
}
