using YekAbr.Domain.Enums;

namespace YekAbr.Services.DTOs.Transfers;

public sealed class ProviderSyncOperationDto
{
    public Guid Id { get; set; }
    public ProviderSyncOperationStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public CloudProviderType SourceProviderType { get; set; }
    public CloudProviderType DestinationProviderType { get; set; }
    public string? SourceProviderName { get; set; }
    public string? DestinationProviderName { get; set; }
    public Guid SourceConnectedCloudAccountId { get; set; }
    public Guid DestinationConnectedCloudAccountId { get; set; }
    public string? SourceAccountDisplayName { get; set; }
    public string? DestinationAccountDisplayName { get; set; }
    public int TotalFiles { get; set; }
    public int SucceededFiles { get; set; }
    public int FailedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
