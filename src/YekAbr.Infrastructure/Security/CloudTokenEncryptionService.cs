using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Infrastructure.Security;

/// <summary>
/// Encrypts cloud tokens using ASP.NET Core Data Protection.
/// Key rotation is handled by the Data Protection key ring.
/// </summary>
public sealed class CloudTokenEncryptionService : ICloudTokenEncryptionService
{
    private readonly IDataProtector _protector;

    public CloudTokenEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<CloudTokenEncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(options);

        var purpose = string.IsNullOrWhiteSpace(options.Value.Purpose)
            ? "YekAbr.CloudProviderTokens.v1"
            : options.Value.Purpose;

        _protector = dataProtectionProvider.CreateProtector(purpose);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new ArgumentException("مقدار توکن برای رمزنگاری نمی‌تواند خالی باشد.", nameof(plainText));
        }

        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            throw new ArgumentException("مقدار رمزشده برای رمزگشایی نمی‌تواند خالی باشد.", nameof(cipherText));
        }

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (Exception exception) when (exception is not ArgumentException)
        {
            throw new InvalidOperationException("رمزگشایی توکن ابری ناموفق بود.", exception);
        }
    }
}
