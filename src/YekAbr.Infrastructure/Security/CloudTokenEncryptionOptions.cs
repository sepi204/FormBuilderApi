namespace YekAbr.Infrastructure.Security;

public sealed class CloudTokenEncryptionOptions
{
    public const string SectionName = "CloudTokenEncryption";

    /// <summary>
    /// Data Protection purpose used for cloud token protect/unprotect.
    /// Changing this value invalidates previously encrypted tokens.
    /// </summary>
    public string Purpose { get; set; } = "YekAbr.CloudProviderTokens.v1";
}
