using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CosmoWhisper.Managers
{
    public static class SecurityManager
    {
        private const int KeySize = 256;
        private const int Iterations = 10000;

        public static void EncryptFile(string inputFile, string outputFile, string password)
        {
            byte[] salt = GenerateRandomBytes(16);
            byte[] iv = GenerateRandomBytes(16);

            using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = keyDerivation.GetBytes(KeySize / 8);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var outStream = new FileStream(outputFile, FileMode.Create))
                    {
                        outStream.Write(salt, 0, salt.Length);
                        outStream.Write(iv, 0, iv.Length);

                        using (var cryptoStream = new CryptoStream(outStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            using (var inStream = new FileStream(inputFile, FileMode.Open))
                            {
                                inStream.CopyTo(cryptoStream);
                            }
                        }
                    }
                }
            }
        }

        public static void DecryptFile(string inputFile, string outputFile, string password)
        {
            using (var inStream = new FileStream(inputFile, FileMode.Open))
            {
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];

                inStream.Read(salt, 0, 16);
                inStream.Read(iv, 0, 16);

                using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] key = keyDerivation.GetBytes(KeySize / 8);

                    using (var aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;

                        using (var cryptoStream = new CryptoStream(inStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (var outStream = new FileStream(outputFile, FileMode.Create))
                            {
                                cryptoStream.CopyTo(outStream);
                            }
                        }
                    }
                }
            }
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }
    }
}
