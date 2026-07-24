using YekAbr.Domain.Enums;

namespace YekAbr.Services.DTOs.Dashboard;

public sealed class DashboardFileItemResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public CloudProviderType ProviderType { get; set; }
    public string ProviderTypeName { get; set; } = string.Empty;
    public Guid ConnectedCloudAccountId { get; set; }
    public string ProviderFileId { get; set; } = string.Empty;
    public string? ProviderPath { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
