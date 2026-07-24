using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YekAbr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefineSyncMetadataIndexesAndLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadedFileMetadata_ConnectedCloudAccountId",
                table: "UploadedFileMetadata");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_ConnectedCloudAccountId_ProviderFileId",
                table: "UploadedFileMetadata",
                columns: new[] { "ConnectedCloudAccountId", "ProviderFileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadedFileMetadata_ConnectedCloudAccountId_ProviderFileId",
                table: "UploadedFileMetadata");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_ConnectedCloudAccountId",
                table: "UploadedFileMetadata",
                column: "ConnectedCloudAccountId");
        }
    }
}
