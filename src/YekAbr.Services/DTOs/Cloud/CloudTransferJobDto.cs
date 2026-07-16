using YekAbr.Domain.Enums;

namespace YekAbr.Services.DTOs.Cloud;

public sealed class CloudTransferJobDto
{
    public Guid Id { get; set; }
    public CloudTransferStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public Guid SourceConnectedAccountId { get; set; }
    public Guid DestinationConnectedAccountId { get; set; }
    public string? SourceAccountDisplayName { get; set; }
    public string? DestinationAccountDisplayName { get; set; }
    public CloudProviderType? SourceProvider { get; set; }
    public CloudProviderType? DestinationProvider { get; set; }
    public string? SourceProviderName { get; set; }
    public string? DestinationProviderName { get; set; }
    public string SourceItemId { get; set; } = string.Empty;
    public string SourceItemName { get; set; } = string.Empty;
    public CloudItemType SourceItemType { get; set; }
    public string? DestinationParentFolderId { get; set; }
    public int ProgressPercentage { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public long? TotalBytes { get; set; }
    public long? TransferredBytes { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsCancellationRequested { get; set; }
}
