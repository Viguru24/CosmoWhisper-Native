using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace CosmoSetup
{
    public partial class MainWindow : Window
    {
        private const string AppName = "CosmoWhisper";
        private const string ExeName = "CosmoWhisperNative.exe";
        private string InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CosmoWhisper");

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;
            StatusText.Text = "Starting installation...";
            
            try
            {
                await Task.Run(() => PerformInstallation());
                StatusText.Text = "Installation Successful!";
                InstallProgress.Value = 100;
                
                await Task.Delay(1000);
                
                // Launch the app
                Process.Start(new ProcessStartInfo(Path.Combine(InstallPath, ExeName)) { UseShellExecute = true });
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed.";
                InstallButton.IsEnabled = true;
            }
        }

        private void PerformInstallation()
        {
            // 1. Kill old processes
            UpdateStatus("Stopping old versions...", 10);
            foreach (var process in Process.GetProcessesByName("CosmoWhisperNative").Concat(Process.GetProcessesByName("CosmoWhisper")))
            {
                try { process.Kill(); } catch { }
            }

            // 2. Prepare directory
            UpdateStatus("Preparing folders...", 20);
            if (Directory.Exists(InstallPath))
            {
                try { Directory.Delete(InstallPath, true); } catch { }
            }
            Directory.CreateDirectory(InstallPath);

        // 3. Extract embedded payload
            UpdateStatus("Extracting files...", 40);
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("CosmoSetup.payload.zip"))
                {
                    if (stream == null) throw new Exception("Embedded payload not found!");
                    
                    using (var archive = new System.IO.Compression.ZipArchive(stream))
                    {
                         // We can't use .ExtractToDirectory because we need overwrite permission
                         // and also we might be in use if not careful. But we cleaned dir already.
                         
                         foreach (var entry in archive.Entries)
                         {
                             // Create directory
                             string completeFileName = Path.Combine(InstallPath, entry.FullName);
                             
                             // Standard Zip format uses forward slashes.
                             if (entry.Name == "") 
                             {
                                 // It's a directory
                                 Directory.CreateDirectory(completeFileName);
                                 continue;
                             }
                             
                             // Ensure directory exists for file
                             string directory = Path.GetDirectoryName(completeFileName);
                             if (!string.IsNullOrEmpty(directory))
                                 Directory.CreateDirectory(directory);
                                 
                             entry.ExtractToFile(completeFileName, true);
                         }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Extraction failed: {ex.Message}");
            }

            // 4. Registry - Protocol & Uninstall
            UpdateStatus("Registering with Windows...", 70);
            RegisterProtocol();
            RegisterUninstall();
            SetStartup();

            // 5. Shortcuts
            UpdateStatus("Creating shortcuts...", 90);
            CreateShortcut();
        }



        private void RegisterProtocol()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\cosmowhisper"))
            {
                key.SetValue("", "URL:CosmoWhisper Protocol");
                key.SetValue("URL Protocol", "");
                using (var icon = key.CreateSubKey("DefaultIcon")) icon.SetValue("", $"\"{InstallPath}\\{ExeName}\",1");
                using (var cmd = key.CreateSubKey(@"shell\open\command")) cmd.SetValue("", $"\"{InstallPath}\\{ExeName}\" \"%1\"");
            }
        }

        private void RegisterUninstall()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CosmoWhisper"))
            {
                key.SetValue("DisplayName", "CosmoWhisper");
                key.SetValue("UninstallString", "powershell.exe -ExecutionPolicy Bypass -File \"" + Path.Combine(InstallPath, "Uninstall.ps1") + "\"");
                key.SetValue("DisplayIcon", Path.Combine(InstallPath, "app.ico"));
                key.SetValue("Publisher", "Cosmo");
                key.SetValue("DisplayVersion", "2.2.0");
            }
        }

        private void SetStartup()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                key.SetValue(AppName, $"\"{InstallPath}\\{ExeName}\"");
            }
        }

        private void CreateShortcut()
        {
            string commonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\CosmoWhisper.lnk");
            string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CosmoWhisper.lnk");

            CreateLnk(commonPath);
            CreateLnk(desktopPath);
        }

        private void CreateLnk(string path)
        {
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            try
            {
                var lnk = shell.CreateShortcut(path);
                lnk.TargetPath = Path.Combine(InstallPath, ExeName);
                lnk.WorkingDirectory = InstallPath;
                lnk.IconLocation = Path.Combine(InstallPath, "app.ico");
                lnk.Save();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
            }
        }

        private void UpdateStatus(string text, double progress)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = text;
                InstallProgress.Value = progress;
            });
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}