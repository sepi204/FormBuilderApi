using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YekAbr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudTransferJobProgressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationRequestedAtUtc",
                table: "CloudTransferJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedItems",
                table: "CloudTransferJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalItems",
                table: "CloudTransferJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "CloudTransferJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CloudTransferJobs_Status_CreatedAtUtc",
                table: "CloudTransferJobs",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CloudTransferJobs_Status_CreatedAtUtc",
                table: "CloudTransferJobs");

            migrationBuilder.DropColumn(
                name: "CancellationRequestedAtUtc",
                table: "CloudTransferJobs");

            migrationBuilder.DropColumn(
                name: "ProcessedItems",
                table: "CloudTransferJobs");

            migrationBuilder.DropColumn(
                name: "TotalItems",
                table: "CloudTransferJobs");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "CloudTransferJobs");
        }
    }
}
