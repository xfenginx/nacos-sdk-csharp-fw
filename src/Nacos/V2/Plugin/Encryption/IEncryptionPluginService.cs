namespace Nacos.V2.Plugin.Encryption
{
    public interface IEncryptionPluginService
    {
        string Encrypt(string secretKey, string content);

        string Decrypt(string secretKey, string content);

        string GenerateSecretKey();

        string AlgorithmName();

        string EncryptSecretKey(string secretKey);

        string DecryptSecretKey(string secretKey);
    }
}
