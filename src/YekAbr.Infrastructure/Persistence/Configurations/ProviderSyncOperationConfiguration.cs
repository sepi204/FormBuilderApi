using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YekAbr.Domain.Entities;
using YekAbr.Infrastructure.Identity;

namespace YekAbr.Infrastructure.Persistence.Configurations;

public sealed class ProviderSyncOperationConfiguration : IEntityTypeConfiguration<ProviderSyncOperation>
{
    public void Configure(EntityTypeBuilder<ProviderSyncOperation> builder)
    {
        builder.ToTable("ProviderSyncOperations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.SourceProviderType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.DestinationProviderType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SourceConnectedCloudAccount)
            .WithMany()
            .HasForeignKey(x => x.SourceConnectedCloudAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationConnectedCloudAccount)
            .WithMany()
            .HasForeignKey(x => x.DestinationConnectedCloudAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
