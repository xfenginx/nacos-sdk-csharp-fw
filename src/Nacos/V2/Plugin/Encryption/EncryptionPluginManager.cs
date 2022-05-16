namespace Nacos.V2.Plugin.Encryption
{
    using System.Linq;

    public class EncryptionPluginManager
    {
        private readonly System.Collections.Generic.IEnumerable<IEncryptionPluginService> _encryptionPluginServices;

        public EncryptionPluginManager(System.Collections.Generic.IEnumerable<IEncryptionPluginService> encryptionPluginServices)
        {
            this._encryptionPluginServices = encryptionPluginServices;
        }

        public IEncryptionPluginService FindEncryptionService(string algorithmName)
            => _encryptionPluginServices.FirstOrDefault(x => x.AlgorithmName().Equals(algorithmName));
    }
}
