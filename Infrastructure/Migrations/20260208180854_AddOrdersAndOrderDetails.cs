using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndOrderDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ordini",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DataOrdine = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoPagamento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroCarta = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TotaleOrdine = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StatoOrdine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordini", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Ordini_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DettaglioOrdine",
                columns: table => new
                {
                    DettaglioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProdottoId = table.Column<int>(type: "int", nullable: false),
                    Quantita = table.Column<int>(type: "int", nullable: false),
                    PrezzoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DettaglioOrdine", x => x.DettaglioId);
                    table.ForeignKey(
                        name: "FK_DettaglioOrdine_Ordini_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Ordini",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DettaglioOrdine_OrderId",
                table: "DettaglioOrdine",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_DataOrdine",
                table: "Ordini",
                column: "DataOrdine");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_TipoPagamento",
                table: "Ordini",
                column: "TipoPagamento");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_UserId",
                table: "Ordini",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DettaglioOrdine");

            migrationBuilder.DropTable(
                name: "Ordini");
        }
    }
}
