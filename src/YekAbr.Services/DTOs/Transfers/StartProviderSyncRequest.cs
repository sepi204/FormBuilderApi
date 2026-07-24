using YekAbr.Domain.Enums;

namespace YekAbr.Services.DTOs.Transfers;

public sealed class StartProviderSyncRequest
{
    public CloudProviderType SourceProvider { get; set; }
    public CloudProviderType DestinationProvider { get; set; }
}
