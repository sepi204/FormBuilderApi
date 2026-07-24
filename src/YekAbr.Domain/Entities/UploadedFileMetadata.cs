using YekAbr.Domain.Enums;

namespace YekAbr.Domain.Entities;

/// <summary>
/// Application-owned metadata for a file the user uploaded to a connected cloud provider.
/// The binary content remains on the provider; only metadata is stored locally.
/// </summary>
public sealed class UploadedFileMetadata
{
    public Guid Id { get; set; }

    /// <summary>
    /// Matches ASP.NET Identity AppUser.Id (string).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public Guid ConnectedCloudAccountId { get; set; }

    /// <summary>
    /// Display / current file name as returned by the provider after upload.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Original client file name at upload time.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public CloudProviderType ProviderType { get; set; }

    /// <summary>
    /// Provider-specific file identifier (opaque to the application).
    /// </summary>
    public string ProviderFileId { get; set; } = string.Empty;

    /// <summary>
    /// Optional provider path or display path when available.
    /// </summary>
    public string? ProviderPath { get; set; }

    /// <summary>
    /// Relative application download route when available
    /// (e.g. /api/cloud/accounts/{accountId}/files/{providerFileId}/download).
    /// </summary>
    public string? DownloadUrl { get; set; }

    public string? ThumbnailUrl { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }

    /// <summary>
    /// Last time this metadata row was refreshed from an external provider sync.
    /// </summary>
    public DateTime? LastSyncedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
}
