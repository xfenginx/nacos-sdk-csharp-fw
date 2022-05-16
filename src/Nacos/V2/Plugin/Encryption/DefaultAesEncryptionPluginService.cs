namespace Nacos.V2.Plugin.Encryption
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class DefaultAesEncryptionPluginService : IEncryptionPluginService
    {
        public static readonly string AES_NAME = "aes";

        private static readonly string IV_PARAMETER = "fa6fa5207b3286b2";

        private static readonly string DEFAULT_SECRET_KEY = "nacos6b31e19f931a7603ae5473250b4";

        private static readonly int IV_LENGTH = 16;

        public string AlgorithmName() => AES_NAME;

        public string Decrypt(string secretKey, string content)
        {
            throw new NotImplementedException();
        }

        public string DecryptSecretKey(string secretKey) => secretKey;

        public string Encrypt(string secretKey, string content)
        {
            if (string.IsNullOrWhiteSpace(secretKey)) return content;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(content);

                var encryptBytes = AESEncrypt(plainBytes, secretKey, GenerateIv(secretKey));
                if (encryptBytes == null)
                {
                    return null;
                }

                return BitConverter.ToString(encryptBytes).Replace("-", "");
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public string EncryptSecretKey(string secretKey) => secretKey;

        public string GenerateSecretKey()
        {
            return DEFAULT_SECRET_KEY;
        }

        private byte[] GenerateIv(string secretKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < IV_LENGTH)
            {
                return Encoding.UTF8.GetBytes(IV_PARAMETER.PadRight(IV_LENGTH));
            }

            string iv = secretKey.Substring(0, IV_LENGTH);
            return Encoding.UTF8.GetBytes(iv);
        }

        private static byte[] AESEncrypt(byte[] data, string key, byte[] bVector)
        {
            byte[] plainBytes = data;
            byte[] bKey = new byte[32];
            Array.Copy(Encoding.UTF8.GetBytes(key.PadRight(bKey.Length)), bKey, bKey.Length);

            byte[] encryptData = null; // encrypted data
            using (Aes aes = Aes.Create())
            {
                try
                {
                    using (MemoryStream memory = new MemoryStream())
                    {
                        using (CryptoStream encryptor = new CryptoStream(memory, aes.CreateEncryptor(bKey, bVector), CryptoStreamMode.Write))
                        {
                            encryptor.Write(plainBytes, 0, plainBytes.Length);
                            encryptor.FlushFinalBlock();

                            encryptData = memory.ToArray();
                        }
                    }
                }
                catch
                {
                    encryptData = null;
                }

                return encryptData;
            }
        }

        public static byte[] AESDecrypt(byte[] data, string key, byte[] bVector)
        {
            byte[] encryptedBytes = data;
            byte[] bKey = new byte[32];
            Array.Copy(Encoding.UTF8.GetBytes(key.PadRight(bKey.Length)), bKey, bKey.Length);

            byte[] decryptedData = null; // decrypted data

            using (Aes aes = Aes.Create())
            {
                try
                {
                    using (MemoryStream memory = new MemoryStream(encryptedBytes))
                    {
                        using (CryptoStream decryptor = new CryptoStream(memory, aes.CreateDecryptor(bKey, bVector), CryptoStreamMode.Read))
                        {
                            using (MemoryStream tempMemory = new MemoryStream())
                            {
                                byte[] buffer = new byte[1024];
                                Int32 readBytes = 0;
                                while ((readBytes = decryptor.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    tempMemory.Write(buffer, 0, readBytes);
                                }

                                decryptedData = tempMemory.ToArray();
                            }
                        }
                    }
                }
                catch
                {
                    decryptedData = null;
                }

                return decryptedData;
            }
        }
    }
}
