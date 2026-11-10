using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Charmly.Installer;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        Program.Log("App.OnStartup called");
        try
        {
            if (e.Args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
            {
                PerformUninstall(silent: e.Args.Contains("--quiet", StringComparer.OrdinalIgnoreCase));
                Shutdown();
                return;
            }

            Program.Log("Instantiating MainWindow...");
            var main = new MainWindow();
            Program.Log("Showing MainWindow...");
            main.Show();
            Program.Log("MainWindow.Show() called.");
        }
        catch (Exception ex)
        {
            Program.Log($"Error in OnStartup: {ex}");
            MessageBox.Show($"Failed to launch installer: {ex.Message}\n\n{ex.StackTrace}", "Charmly Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void PerformUninstall(bool silent)
    {
        if (!silent)
        {
            var res = MessageBox.Show(
                "Are you sure you want to remove Charmly from your computer?",
                "Uninstall Charmly",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            // Stop any running Charmly/Hangly process
            foreach (var proc in System.Diagnostics.Process.GetProcessesByName("Charmly").Concat(System.Diagnostics.Process.GetProcessesByName("Hangly")))
            {
                try { proc.Kill(); proc.WaitForExit(3000); } catch { }
            }

            // Remove Desktop Shortcut
            string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Charmly.lnk");
            if (File.Exists(desktopShortcut)) File.Delete(desktopShortcut);

            // Remove Start Menu Shortcut
            string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            string startMenuShortcut = Path.Combine(startMenuDir, "Charmly.lnk");
            if (File.Exists(startMenuShortcut)) File.Delete(startMenuShortcut);

            // Remove Startup Registry Run Key
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                runKey?.DeleteValue("Charmly", throwOnMissingValue: false);
                runKey?.DeleteValue("Hangly", throwOnMissingValue: false);
            }
            catch { }

            // Remove Uninstall Registry Key
            try
            {
                using var uninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", writable: true);
                uninstallKey?.DeleteSubKeyTree("Charmly", throwOnMissingSubKey: false);
            }
            catch { }

            // Delete program directory via background cmd delayed deletion so this uninstaller exe can exit
            string installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            if (Directory.Exists(installDir) && installDir.Contains("Programs", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout /t 1 /nobreak > NUL & rmdir /s /q \"{installDir}\"",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }

            if (!silent)
            {
                MessageBox.Show(
                    "Charmly was successfully removed from your computer.",
                    "Uninstall Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                MessageBox.Show($"Uninstall error: {ex.Message}", "Uninstall Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
