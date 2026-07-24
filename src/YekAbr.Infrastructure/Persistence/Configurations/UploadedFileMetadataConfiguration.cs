using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YekAbr.Domain.Entities;
using YekAbr.Infrastructure.Identity;

namespace YekAbr.Infrastructure.Persistence.Configurations;

public sealed class UploadedFileMetadataConfiguration : IEntityTypeConfiguration<UploadedFileMetadata>
{
    public void Configure(EntityTypeBuilder<UploadedFileMetadata> builder)
    {
        builder.ToTable("UploadedFileMetadata");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.ConnectedCloudAccountId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Extension)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.ContentType)
            .HasMaxLength(256);

        builder.Property(x => x.Size)
            .IsRequired();

        builder.Property(x => x.ProviderType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ProviderFileId)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ProviderPath)
            .HasMaxLength(2000);

        builder.Property(x => x.DownloadUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.ThumbnailUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.UploadedAtUtc)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.UploadedAtUtc);
        builder.HasIndex(x => x.ProviderType);
        builder.HasIndex(x => new { x.UserId, x.IsDeleted, x.UploadedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.ConnectedCloudAccountId, x.ProviderFileId });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ConnectedCloudAccount>()
            .WithMany()
            .HasForeignKey(x => x.ConnectedCloudAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
