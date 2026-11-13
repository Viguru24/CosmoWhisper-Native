using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CosmoWhisper.Managers
{
    /// <summary>
    /// Secure Backup Manager.
    /// Uses AES-256 encryption to secure backup archives.
    /// </summary>
    public class VaultManager
    {
        private static VaultManager _instance;
        public static VaultManager Shared => _instance ??= new VaultManager();

        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits
        private const int IvSize = 16;   // 128 bits
        private const int Iterations = 100000; // PBKDF2 iterations

        public async Task<string> CreateBackupAsync(string name, string password, string destinationDir)
        {
            string sourceDir = PreferenceManager.Shared.AppDataFolder;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            
            string cleanName = string.IsNullOrWhiteSpace(name) ? "CosmoBackup" : name;
            string safeName = string.Join("_", cleanName.Split(Path.GetInvalidFileNameChars()));
            
            // Temporary ZIP path
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"{safeName}_{Guid.NewGuid()}.zip");
            string finalPath = Path.Combine(destinationDir, $"{safeName}_{timestamp}.vault");

            try
            {
                await Task.Run(() =>
                {
                    // 1. Create ZIP
                    if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                    using (var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
                    {
                        if (Directory.Exists(sourceDir))
                        {
                            var filesToBackup = Directory.GetFiles(sourceDir, "*.json", SearchOption.TopDirectoryOnly);
                            foreach (var file in filesToBackup)
                            {
                                string fileName = Path.GetFileName(file);
                                if (fileName.Equals("settings.json", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Sanitize settings.json to remove personal backup paths
                                    try
                                    {
                                        string json = File.ReadAllText(file);
                                        var prefs = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                                        if (prefs != null && prefs.ContainsKey("BackupDirectory"))
                                        {
                                            // Reset to an empty string so the restorer uses their own default
                                            prefs["BackupDirectory"] = ""; 
                                            string sanitizedJson = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
                                            
                                            var entry = archive.CreateEntry(fileName);
                                            using (var writer = new StreamWriter(entry.Open()))
                                            {
                                                writer.Write(sanitizedJson);
                                            }
                                            continue;
                                        }
                                    }
                                    catch { /* Fallback to original if parse fails */ }
                                }
                                
                                archive.CreateEntryFromFile(file, fileName);
                            }
                        }
                    }

                    // 2. Encrypt ZIP if password provided, else just move it (as .zip)
                    if (string.IsNullOrEmpty(password))
                    {
                        // Fallback to unencrypted .zip if no password
                        finalPath = Path.ChangeExtension(finalPath, ".zip");
                        if (File.Exists(finalPath)) File.Delete(finalPath);
                        File.Move(tempZipPath, finalPath);
                    }
                    else
                    {
                        // Encrypt
                        EncryptFile(tempZipPath, finalPath, password);
                        File.Delete(tempZipPath);
                    }
                });

                return finalPath;
            }
            catch (Exception ex)
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                throw new Exception($"Backup failed: {ex.Message}", ex);
            }
        }

        public async Task<string> ExtractToStaging(string backupPath, string password)
        {
            if (!File.Exists(backupPath)) throw new FileNotFoundException("Backup file not found.");

            string stagingDir = Path.Combine(Path.GetTempPath(), "CosmoRestore_Stage_" + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
            Directory.CreateDirectory(stagingDir);

            string tempZipPath = Path.Combine(Path.GetTempPath(), $"restore_temp_{Guid.NewGuid()}.zip");

            try
            {
                await Task.Run(() =>
                {
                    // If .zip, no decryption needed
                    if (Path.GetExtension(backupPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(backupPath, tempZipPath, true);
                    }
                    else
                    {
                        // Decrypt .vault
                        if (string.IsNullOrEmpty(password)) throw new UnauthorizedAccessException("Password required for encrypted backup.");
                        DecryptFile(backupPath, tempZipPath, password);
                    }

                    ZipFile.ExtractToDirectory(tempZipPath, stagingDir);
                });

                if (Directory.GetFiles(stagingDir).Length == 0)
                {
                    throw new Exception("Backup appears to be empty.");
                }

                return stagingDir;
            }
            catch (CryptographicException)
            {
                 throw new UnauthorizedAccessException("Invalid password or corrupted backup.");
            }
            catch (Exception ex)
            {
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
                throw; // Re-throw to controller
            }
            finally
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            }
        }

        private void EncryptFile(string inputFile, string outputFile, string password)
        {
            // Generate random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive key and IV
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = deriveBytes.GetBytes(KeySize);
                byte[] iv = deriveBytes.GetBytes(IvSize);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var fsOut = new FileStream(outputFile, FileMode.Create))
                    {
                        // Write Salt
                        fsOut.Write(salt, 0, salt.Length);
                        // Write IV
                        fsOut.Write(iv, 0, iv.Length);

                        using (var cryptoStream = new CryptoStream(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        using (var fsIn = new FileStream(inputFile, FileMode.Open))
                        {
                            fsIn.CopyTo(cryptoStream);
                        }
                    }
                }
            }
        }

        private void DecryptFile(string inputFile, string outputFile, string password)
        {
            using (var fsIn = new FileStream(inputFile, FileMode.Open))
            {
                // Read Salt
                byte[] salt = new byte[SaltSize];
                if (fsIn.Read(salt, 0, salt.Length) != salt.Length)
                    throw new InvalidDataException("File too short (missing salt).");

                // Read IV
                byte[] iv = new byte[IvSize];
                if (fsIn.Read(iv, 0, iv.Length) != iv.Length)
                    throw new InvalidDataException("File too short (missing IV).");

                // Derive key
                using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] key = deriveBytes.GetBytes(KeySize);
                    
                    // Verify IV matches what we expect? No, IV is random per file.
                    // Just use the IV from file.

                    using (var aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var cryptoStream = new CryptoStream(fsIn, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        using (var fsOut = new FileStream(outputFile, FileMode.Create))
                        {
                            cryptoStream.CopyTo(fsOut);
                        }
                    }
                }
            }
        }

        public List<string> GetAvailableBackups(string directory)
        {
            if (!Directory.Exists(directory)) return new List<string>();
            
            var extensions = new[] { ".zip", ".vault" };
            return Directory.GetFiles(directory)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Select(f => Path.GetFileName(f))
                .ToList();
        }
    }
}


