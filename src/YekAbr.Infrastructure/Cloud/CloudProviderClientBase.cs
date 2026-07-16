using YekAbr.Domain.Enums;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Infrastructure.Cloud;

/// <summary>
/// Shared base for future provider clients. Holds no SDK-specific logic.
/// </summary>
public abstract class CloudProviderClientBase : ICloudProviderClient
{
    public abstract CloudProviderType ProviderType { get; }

    public abstract string ProviderName { get; }
}
