using YekAbr.Domain.Enums;

namespace YekAbr.Domain.Entities;

/// <summary>
/// Tracks a bulk provider-to-provider file copy operation for a user.
/// </summary>
public sealed class ProviderSyncOperation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    public CloudProviderType SourceProviderType { get; set; }
    public CloudProviderType DestinationProviderType { get; set; }

    public Guid SourceConnectedCloudAccountId { get; set; }
    public Guid DestinationConnectedCloudAccountId { get; set; }

    public ProviderSyncOperationStatus Status { get; set; } = ProviderSyncOperationStatus.Pending;

    public int TotalFiles { get; set; }
    public int SucceededFiles { get; set; }
    public int FailedFiles { get; set; }
    public int SkippedFiles { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ConnectedCloudAccount? SourceConnectedCloudAccount { get; set; }
    public ConnectedCloudAccount? DestinationConnectedCloudAccount { get; set; }
}
