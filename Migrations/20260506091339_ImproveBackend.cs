using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmlDetectionApi.Migrations
{
    /// <inheritdoc />
    public partial class ImproveBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockchainLogs_Alerts_AlertId",
                table: "BlockchainLogs");

            migrationBuilder.DropIndex(
                name: "IX_BlockchainLogs_AlertId",
                table: "BlockchainLogs");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "AlertId",
                table: "BlockchainLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "LogType",
                table: "BlockchainLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TransactionId",
                table: "BlockchainLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockchainLogs_AlertId",
                table: "BlockchainLogs",
                column: "AlertId",
                unique: true,
                filter: "[AlertId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BlockchainLogs_TransactionId",
                table: "BlockchainLogs",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockchainLogs_Alerts_AlertId",
                table: "BlockchainLogs",
                column: "AlertId",
                principalTable: "Alerts",
                principalColumn: "AlertId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockchainLogs_Transactions_TransactionId",
                table: "BlockchainLogs",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockchainLogs_Alerts_AlertId",
                table: "BlockchainLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_BlockchainLogs_Transactions_TransactionId",
                table: "BlockchainLogs");

            migrationBuilder.DropIndex(
                name: "IX_BlockchainLogs_AlertId",
                table: "BlockchainLogs");

            migrationBuilder.DropIndex(
                name: "IX_BlockchainLogs_TransactionId",
                table: "BlockchainLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LogType",
                table: "BlockchainLogs");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "BlockchainLogs");

            migrationBuilder.AlterColumn<int>(
                name: "AlertId",
                table: "BlockchainLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockchainLogs_AlertId",
                table: "BlockchainLogs",
                column: "AlertId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BlockchainLogs_Alerts_AlertId",
                table: "BlockchainLogs",
                column: "AlertId",
                principalTable: "Alerts",
                principalColumn: "AlertId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
