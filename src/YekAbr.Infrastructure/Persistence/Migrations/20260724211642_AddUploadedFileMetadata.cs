using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YekAbr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadedFileMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConnectedCloudAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Extension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    ProviderFileId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProviderPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DownloadUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFileMetadata_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploadedFileMetadata_ConnectedCloudAccounts_ConnectedCloudA~",
                        column: x => x.ConnectedCloudAccountId,
                        principalTable: "ConnectedCloudAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_ConnectedCloudAccountId",
                table: "UploadedFileMetadata",
                column: "ConnectedCloudAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_ProviderType",
                table: "UploadedFileMetadata",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_UploadedAtUtc",
                table: "UploadedFileMetadata",
                column: "UploadedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_UserId",
                table: "UploadedFileMetadata",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_UserId_ConnectedCloudAccountId_Provide~",
                table: "UploadedFileMetadata",
                columns: new[] { "UserId", "ConnectedCloudAccountId", "ProviderFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileMetadata_UserId_IsDeleted_UploadedAtUtc",
                table: "UploadedFileMetadata",
                columns: new[] { "UserId", "IsDeleted", "UploadedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadedFileMetadata");
        }
    }
}
