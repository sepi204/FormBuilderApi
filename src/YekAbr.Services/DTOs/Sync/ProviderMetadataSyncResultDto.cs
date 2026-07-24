using YekAbr.Domain.Enums;

namespace YekAbr.Services.DTOs.Sync;

public sealed class ProviderMetadataSyncResultDto
{
    public int ProvidersChecked { get; set; }
    public int ProvidersFailed { get; set; }
    public int FilesDiscovered { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public IReadOnlyList<string> FailedProviderMessages { get; set; } = Array.Empty<string>();
    public IReadOnlyList<CloudProviderType> SyncedProviders { get; set; } = Array.Empty<CloudProviderType>();
}
