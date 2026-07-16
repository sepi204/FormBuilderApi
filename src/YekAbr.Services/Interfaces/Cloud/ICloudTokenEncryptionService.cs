namespace YekAbr.Services.Interfaces.Cloud;

/// <summary>
/// Encrypts and decrypts cloud provider tokens for secure persistence.
/// </summary>
public interface ICloudTokenEncryptionService
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
