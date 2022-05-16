namespace Nacos.V2.Plugin.Encryption
{
    public class EncryptionHandler
    {
        private static readonly string PREFIX = "cipher-";

        private readonly EncryptionPluginManager _manager;

        public EncryptionHandler(EncryptionPluginManager manager)
        {
            this._manager = manager;
        }

        public (string Key, string Value) EncryptHandler(string dataId, string content)
        {
            if (!CheckCipher(dataId)) return ("", content);

            string algorithmName = ParseAlgorithmName(dataId);

            var encryptionPluginService = _manager.FindEncryptionService(algorithmName);
            if (encryptionPluginService == null)
            {
                return ("", content);
            }

            var secretKey = encryptionPluginService.GenerateSecretKey();
            var encryptContent = encryptionPluginService.Encrypt(secretKey, content);

            return (encryptionPluginService.EncryptSecretKey(secretKey), encryptContent);
        }

        public (string Key, string Value) DecryptHandler(string dataId, string secretKey, string content)
        {
            if (!CheckCipher(dataId)) return ("", content);

            string algorithmName = ParseAlgorithmName(dataId);

            var encryptionPluginService = _manager.FindEncryptionService(algorithmName);
            if (encryptionPluginService == null)
            {
                return ("", content);
            }

            var decryptSecretKey = encryptionPluginService.DecryptSecretKey(secretKey);
            var decryptContent = encryptionPluginService.Decrypt(decryptSecretKey, content);

            return (decryptSecretKey, decryptContent);
        }

        private bool CheckCipher(string dataId) => dataId.StartsWith(PREFIX);

        private string ParseAlgorithmName(string dataId) => dataId.Split('-')[1];
    }
}
