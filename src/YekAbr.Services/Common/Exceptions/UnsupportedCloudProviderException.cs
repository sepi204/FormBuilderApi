using YekAbr.Domain.Enums;

namespace YekAbr.Services.Common.Exceptions;

public sealed class UnsupportedCloudProviderException : Exception
{
    public CloudProviderType ProviderType { get; }

    public UnsupportedCloudProviderException(CloudProviderType providerType)
        : base($"پیاده‌سازی ارائه‌دهنده ابری «{providerType}» ثبت نشده است.")
    {
        ProviderType = providerType;
    }
}
