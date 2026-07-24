using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Models;

namespace YekAbr.Infrastructure.Services.Cloud;

/// <summary>
/// Shared helpers for mapping provider file items into local metadata rows.
/// </summary>
internal static class UploadedFileMetadataMapper
{
    public static string BuildDownloadUrl(Guid connectedAccountId, string providerFileId)
    {
        return $"/api/cloud/accounts/{connectedAccountId}/files/{Uri.EscapeDataString(providerFileId)}/download";
    }

    public static UploadedFileMetadata CreateFromCloudItem(
        string userId,
        ConnectedCloudAccount account,
        CloudItem item,
        string? originalFileName = null,
        string? contentTypeOverride = null)
    {
        var fileName = string.IsNullOrWhiteSpace(item.Name)
            ? (originalFileName ?? "file")
            : item.Name.Trim();
        var original = string.IsNullOrWhiteSpace(originalFileName) ? fileName : originalFileName.Trim();
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = Path.GetExtension(original);
        }

        var now = DateTime.UtcNow;
        return new UploadedFileMetadata
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConnectedCloudAccountId = account.Id,
            FileName = fileName,
            OriginalFileName = original,
            Extension = extension.TrimStart('.').ToLowerInvariant(),
            ContentType = string.IsNullOrWhiteSpace(contentTypeOverride) ? item.MimeType : contentTypeOverride,
            Size = item.Size is < 0 or null ? 0 : item.Size.Value,
            ProviderType = account.Provider,
            ProviderFileId = item.Id,
            ProviderPath = item.FullPath,
            DownloadUrl = BuildDownloadUrl(account.Id, item.Id),
            ThumbnailUrl = null,
            UploadedAtUtc = item.ModifiedAtUtc?.ToUniversalTime() ?? now,
            LastModifiedAtUtc = item.ModifiedAtUtc?.ToUniversalTime() ?? now,
            LastSyncedAtUtc = now,
            IsDeleted = false
        };
    }

    public static bool ApplyCloudItemUpdates(UploadedFileMetadata existing, CloudItem item, ConnectedCloudAccount account)
    {
        var changed = false;
        var fileName = string.IsNullOrWhiteSpace(item.Name) ? existing.FileName : item.Name.Trim();
        if (!string.Equals(existing.FileName, fileName, StringComparison.Ordinal))
        {
            existing.FileName = fileName;
            changed = true;
        }

        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (!string.Equals(existing.Extension, extension, StringComparison.Ordinal))
        {
            existing.Extension = extension;
            changed = true;
        }

        var size = item.Size is < 0 or null ? existing.Size : item.Size.Value;
        if (existing.Size != size)
        {
            existing.Size = size;
            changed = true;
        }

        if (!string.Equals(existing.ContentType, item.MimeType, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(item.MimeType))
        {
            existing.ContentType = item.MimeType;
            changed = true;
        }

        if (!string.Equals(existing.ProviderPath, item.FullPath, StringComparison.Ordinal))
        {
            existing.ProviderPath = item.FullPath;
            changed = true;
        }

        var downloadUrl = BuildDownloadUrl(account.Id, item.Id);
        if (!string.Equals(existing.DownloadUrl, downloadUrl, StringComparison.Ordinal))
        {
            existing.DownloadUrl = downloadUrl;
            changed = true;
        }

        var modified = item.ModifiedAtUtc?.ToUniversalTime();
        if (modified.HasValue && existing.LastModifiedAtUtc != modified)
        {
            existing.LastModifiedAtUtc = modified;
            changed = true;
        }

        if (existing.IsDeleted)
        {
            existing.IsDeleted = false;
            changed = true;
        }

        if (existing.ProviderType != account.Provider)
        {
            existing.ProviderType = account.Provider;
            changed = true;
        }

        existing.LastSyncedAtUtc = DateTime.UtcNow;
        return changed;
    }
}
