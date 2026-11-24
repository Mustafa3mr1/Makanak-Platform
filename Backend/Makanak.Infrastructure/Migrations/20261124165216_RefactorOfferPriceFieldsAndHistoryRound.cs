using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Makanak.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOfferPriceFieldsAndHistoryRound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Properties_PropertyId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_PropertyId",
                table: "Offers");

            migrationBuilder.RenameColumn(
                name: "RoundCount",
                table: "Offers",
                newName: "RoundNumber");

            migrationBuilder.RenameColumn(
                name: "CurrentPrice",
                table: "Offers",
                newName: "InitialPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "CounterPrice",
                table: "Offers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoundNumber",
                table: "NegotiationHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Active_Property_Seeker",
                table: "Offers",
                columns: new[] { "PropertyId", "SeekerId" },
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Properties_PropertyId",
                table: "Offers",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Properties_PropertyId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_Active_Property_Seeker",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "CounterPrice",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "RoundNumber",
                table: "NegotiationHistories");

            migrationBuilder.RenameColumn(
                name: "RoundNumber",
                table: "Offers",
                newName: "RoundCount");

            migrationBuilder.RenameColumn(
                name: "InitialPrice",
                table: "Offers",
                newName: "CurrentPrice");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_PropertyId",
                table: "Offers",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Properties_PropertyId",
                table: "Offers",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
