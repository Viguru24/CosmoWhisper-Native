using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CosmoWhisper.Managers
{
    public class VaultManager
    {
        private static VaultManager _instance;
        public static VaultManager Shared => _instance ??= new VaultManager();

        private const string ManifestFileName = "vault_manifest.json";

        public async Task<string> CreateVaultAsync(string name, string password, string destinationDir)
        {
            string sourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CosmoWhisper");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            string safeName = string.IsNullOrWhiteSpace(name) ? "CosmoVault" : string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            string finalVaultPath = Path.Combine(destinationDir, $"{safeName}_{timestamp}.vault");

            // 1. Create Staging Area
            string stagingDir = Path.Combine(Path.GetTempPath(), "CosmoVault_Export_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagingDir);

            try
            {
                // 2. Prepare files for ZIP
                string tempZip = Path.Combine(Path.GetTempPath(), "vault_stage_" + Guid.NewGuid().ToString("N") + ".zip");
                
                using (var archive = ZipFile.Open(tempZip, ZipArchiveMode.Create))
                {
                    if (Directory.Exists(sourceDir))
                    {
                        var filesToBackup = Directory.GetFiles(sourceDir, "*.json", SearchOption.TopDirectoryOnly);
                        foreach (var file in filesToBackup)
                        {
                            archive.CreateEntryFromFile(file, Path.GetFileName(file));
                        }
                    }
                }

                // 3. Encrypt the ZIP package
                await Task.Run(() => SecurityManager.EncryptFile(tempZip, finalVaultPath, password));

                // 4. Cleanup temp zip
                if (File.Exists(tempZip)) File.Delete(tempZip);

                return finalVaultPath;
            }
            finally
            {
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
            }
        }

        public async Task<(bool success, string message)> VerifyVault(string vaultPath, string password)
        {
            if (!File.Exists(vaultPath)) return (false, "Vault file not found.");

            string tempDir = Path.Combine(Path.GetTempPath(), "CosmoVault_Verify_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string tempZip = Path.Combine(tempDir, "data.zip");

            try
            {
                // Decrypt to staging ZIP - this will throw if password/format is wrong
                await Task.Run(() => SecurityManager.DecryptFile(vaultPath, tempZip, password));

                // Verify ZIP integrity by opening it
                using (var archive = ZipFile.OpenRead(tempZip))
                {
                    if (archive.Entries.Count == 0) return (false, "Vault is empty.");
                }
                return (true, "Verified");
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return (false, "Incorrect Password");
            }
            catch (InvalidDataException)
            {
                return (false, "Vault file is corrupted (Invalid Zip).");
            }
            catch (Exception ex)
            {
                return (false, $"Verification Error: {ex.Message}");
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        public async Task<string> ExtractToStaging(string vaultPath, string password)
        {
            if (!File.Exists(vaultPath)) throw new FileNotFoundException("Vault file not found.");

            string stagingDir = Path.Combine(Path.GetTempPath(), "CosmoWhisper_RestoreStaging");
            // Clean previous staging if exists
            if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
            Directory.CreateDirectory(stagingDir);

            string tempZipDir = Path.Combine(Path.GetTempPath(), "CosmoVault_Decrypt_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempZipDir);
            string tempZip = Path.Combine(tempZipDir, "data.zip");

            try
            {
                // Decrypt
                await Task.Run(() => SecurityManager.DecryptFile(vaultPath, tempZip, password));

                // Extract to Staging
                ZipFile.ExtractToDirectory(tempZip, stagingDir);

                return stagingDir;
            }
            finally
            {
                if (Directory.Exists(tempZipDir)) Directory.Delete(tempZipDir, true);
            }
        }


        public List<string> GetAvailableVaults(string directory)
        {
            if (!Directory.Exists(directory)) return new List<string>();
            
            return Directory.GetFiles(directory, "*.vault")
                .OrderByDescending(f => f)
                .Select(f => Path.GetFileName(f))
                .ToList();
        }
    }
}

