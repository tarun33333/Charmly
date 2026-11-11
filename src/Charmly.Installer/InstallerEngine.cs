using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace Charmly.Installer;

public static class InstallerEngine
{
    public static void Install(
        string targetDir,
        bool enableStartup,
        bool createDesktopShortcut,
        bool createStartMenuShortcut,
        Action<int, string>? onProgress = null)
    {
        Directory.CreateDirectory(targetDir);

        // Terminate any running Charmly or Hangly instance so files can be updated
        onProgress?.Invoke(5, "Closing running Charmly instances...");
        foreach (var proc in Process.GetProcessesByName("Charmly").Concat(Process.GetProcessesByName("Hangly")))
        {
            try
            {
                proc.Kill();
                proc.WaitForExit(3000);
            }
            catch { }
        }

        // Locate payload zip (either embedded resource or adjacent zip file)
        Stream? zipStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip");

        if (zipStream == null)
        {
            string adjacentZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Charmly-v0.9.2-Windows-x64.zip");
            if (!File.Exists(adjacentZip))
            {
                adjacentZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Charmly-v0.9.1-Windows-x64.zip");
            }
            if (!File.Exists(adjacentZip))
            {
                adjacentZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\publish\Charmly-v0.9.2-Windows-x64.zip");
            }
            if (File.Exists(adjacentZip))
            {
                zipStream = File.OpenRead(adjacentZip);
            }
        }

        if (zipStream == null)
        {
            throw new FileNotFoundException("Could not find the application installation payload.");
        }

        using (zipStream)
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            int total = archive.Entries.Count;
            int count = 0;

            foreach (var entry in archive.Entries)
            {
                count++;
                int pct = 10 + (int)((count / (float)total) * 80);

                onProgress?.Invoke(pct, $"Extracting {entry.Name}...");

                string destinationPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                if (!destinationPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Zip Slip protection
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }

        onProgress?.Invoke(92, "Creating shortcuts and registering uninstaller...");

        string exePath = Path.Combine(targetDir, "Charmly.exe");
        if (!File.Exists(exePath))
        {
            exePath = Path.Combine(targetDir, "Hangly.exe");
        }

        // Copy this installer as uninstaller into the target directory
        try
        {
            string? currentExe = Environment.ProcessPath;
            string uninstallerPath = Path.Combine(targetDir, "Uninstall.exe");
            if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe))
            {
                File.Copy(currentExe, uninstallerPath, overwrite: true);
            }
        }
        catch { }

        // Create Start Menu Shortcut
        if (createStartMenuShortcut)
        {
            try
            {
                string startMenuPrograms = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs");
                Directory.CreateDirectory(startMenuPrograms);
                string startMenuLnk = Path.Combine(startMenuPrograms, "Charmly.lnk");
                ShortcutHelper.CreateShortcut(startMenuLnk, exePath, "Charmly by Tarun");
            }
            catch { }
        }

        // Create Desktop Shortcut
        if (createDesktopShortcut)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string desktopLnk = Path.Combine(desktopPath, "Charmly.lnk");
                ShortcutHelper.CreateShortcut(desktopLnk, exePath, "Charmly by Tarun");
            }
            catch { }
        }

        // Configure Windows Auto-Start / Launch at Login
        if (enableStartup)
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                runKey?.SetValue("Charmly", $"\"{exePath}\" --login");
                runKey?.SetValue("Hangly", $"\"{exePath}\" --login");
            }
            catch { }
        }

        // Register Uninstaller in Windows Add/Remove Programs
        try
        {
            using var uninstKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Charmly", writable: true);
            if (uninstKey != null)
            {
                uninstKey.SetValue("DisplayName", "Charmly");
                uninstKey.SetValue("DisplayVersion", "0.9.2");
                uninstKey.SetValue("Publisher", "Tarun");
                uninstKey.SetValue("DisplayIcon", $"{exePath},0");
                uninstKey.SetValue("InstallLocation", targetDir);
                uninstKey.SetValue("UninstallString", $"\"{Path.Combine(targetDir, "Uninstall.exe")}\" --uninstall");
                uninstKey.SetValue("QuietUninstallString", $"\"{Path.Combine(targetDir, "Uninstall.exe")}\" --uninstall --quiet");
                uninstKey.SetValue("URLInfoAbout", "https://itstarun.me");
                uninstKey.SetValue("HelpLink", "https://github.com/tarun33333/Charmly");
                uninstKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                uninstKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }
        catch { }

        onProgress?.Invoke(100, "Installation complete!");
    }
}
