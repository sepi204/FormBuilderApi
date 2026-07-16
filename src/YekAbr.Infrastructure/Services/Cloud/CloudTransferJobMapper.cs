using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Services.DTOs.Cloud;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Infrastructure.Services.Cloud;

internal static class CloudTransferJobMapper
{
    public static CloudTransferJobDto Map(CloudTransferJob job, ICloudProviderClientFactory? providerFactory = null)
    {
        return new CloudTransferJobDto
        {
            Id = job.Id,
            Status = job.Status,
            StatusDisplayName = ToPersianStatus(job.Status),
            SourceConnectedAccountId = job.SourceConnectedCloudAccountId,
            DestinationConnectedAccountId = job.DestinationConnectedCloudAccountId,
            SourceAccountDisplayName = job.SourceConnectedCloudAccount?.DisplayName,
            DestinationAccountDisplayName = job.DestinationConnectedCloudAccount?.DisplayName,
            SourceProvider = job.SourceConnectedCloudAccount?.Provider,
            DestinationProvider = job.DestinationConnectedCloudAccount?.Provider,
            SourceProviderName = ResolveProviderName(job.SourceConnectedCloudAccount?.Provider, providerFactory),
            DestinationProviderName = ResolveProviderName(job.DestinationConnectedCloudAccount?.Provider, providerFactory),
            SourceItemId = job.SourceItemId,
            SourceItemName = job.SourceItemName,
            SourceItemType = job.SourceItemType,
            DestinationParentFolderId = job.DestinationParentFolderId,
            ProgressPercentage = job.ProgressPercentage,
            TotalItems = job.TotalItems,
            ProcessedItems = job.ProcessedItems,
            TotalBytes = job.TotalBytes,
            TransferredBytes = job.TransferredBytes,
            FailureReason = job.FailureReason,
            CreatedAtUtc = job.CreatedAtUtc,
            UpdatedAtUtc = job.UpdatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            IsCancellationRequested = job.CancellationRequestedAtUtc.HasValue
        };
    }

    public static string ToPersianStatus(CloudTransferStatus status) => status switch
    {
        CloudTransferStatus.Pending => "در انتظار",
        CloudTransferStatus.Running => "در حال اجرا",
        CloudTransferStatus.Completed => "تکمیل‌شده",
        CloudTransferStatus.Failed => "ناموفق",
        CloudTransferStatus.Cancelled => "لغوشده",
        _ => status.ToString()
    };

    private static string? ResolveProviderName(CloudProviderType? provider, ICloudProviderClientFactory? factory)
    {
        if (!provider.HasValue)
        {
            return null;
        }

        if (factory is not null && factory.IsSupported(provider.Value))
        {
            return factory.GetProvider(provider.Value).ProviderName;
        }

        return provider.Value.ToString();
    }
}
