using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YekAbr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderSyncAndMetadataSyncSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadedFileMetadata_UserId_ConnectedCloudAccountId_Provide~",
                table: "UploadedFileMetadata");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "UploadedFileMetadata",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderSyncOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SourceProviderType = table.Column<int>(type: "integer", nullable: false),
                    DestinationProviderType = table.Column<int>(type: "integer", nullable: false),
                    SourceConnectedCloudAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationConnectedCloudAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalFiles = table.Column<int>(type: "integer", nullable: false),
                    SucceededFiles = table.Column<int>(type: "integer", nullable: false),
                    FailedFiles = table.Column<int>(type: "integer", nullable: false),
                    SkippedFiles = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderSyncOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderSyncOperations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderSyncOperations_ConnectedCloudAccounts_DestinationCo~",
                        column: x => x.DestinationConnectedCloudAccountId,
                        principalTable: "ConnectedCloudAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderSyncOperations_ConnectedCloudAccounts_SourceConnect~",
                        column: x => x.SourceConnectedCloudAccountId,
                        principalTable: "ConnectedCloudAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_UserId_ConnectedCloudAccountId_Provide~",
                table: "UploadedFileMetadata",
                columns: new[] { "UserId", "ConnectedCloudAccountId", "ProviderFileId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSyncOperations_DestinationConnectedCloudAccountId",
                table: "ProviderSyncOperations",
                column: "DestinationConnectedCloudAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSyncOperations_SourceConnectedCloudAccountId",
                table: "ProviderSyncOperations",
                column: "SourceConnectedCloudAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSyncOperations_Status",
                table: "ProviderSyncOperations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSyncOperations_Status_CreatedAtUtc",
                table: "ProviderSyncOperations",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSyncOperations_UserId",
                table: "ProviderSyncOperations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSyncOperations_UserId_CreatedAtUtc",
                table: "ProviderSyncOperations",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderSyncOperations");

            migrationBuilder.DropIndex(
                name: "IX_UploadedFileMetadata_UserId_ConnectedCloudAccountId_Provide~",
                table: "UploadedFileMetadata");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "UploadedFileMetadata");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_UserId_ConnectedCloudAccountId_Provide~",
                table: "UploadedFileMetadata",
                columns: new[] { "UserId", "ConnectedCloudAccountId", "ProviderFileId" });
        }
    }
}
