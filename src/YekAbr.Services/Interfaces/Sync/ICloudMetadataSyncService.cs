using YekAbr.Domain.Enums;
using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Sync;

namespace YekAbr.Services.Interfaces.Sync;

public interface ICloudMetadataSyncService
{
    Task<Result<ProviderMetadataSyncResultDto>> SyncAllConnectedProvidersAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Result<ProviderMetadataSyncResultDto>> SyncProviderAsync(
        string userId,
        CloudProviderType providerType,
        CancellationToken cancellationToken = default);
}
