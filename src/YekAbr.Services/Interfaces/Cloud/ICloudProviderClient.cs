using YekAbr.Domain.Enums;

namespace YekAbr.Services.Interfaces.Cloud;

/// <summary>
/// Provider-agnostic contract for cloud storage integrations.
/// Concrete SDK implementations are registered in later phases.
/// </summary>
public interface ICloudProviderClient
{
    CloudProviderType ProviderType { get; }

    string ProviderName { get; }
}
