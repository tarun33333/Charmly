using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Charmly.Installer;

public static class Program
{
    private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "charmly_setup.log");

    public static void Log(string msg)
    {
        try
        {
            File.AppendAllText(LogFile, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}");
        }
        catch { }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        Log("Program.Main started. Args: " + string.Join(" ", args));

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Log($"UnhandledException: {e.ExceptionObject}");
        };

        // Handle Silent Update / In-app Update invocation
        if (args.Any(a => a.Equals("/SILENT", StringComparison.OrdinalIgnoreCase) ||
                          a.Equals("/UPDATE", StringComparison.OrdinalIgnoreCase) ||
                          a.Equals("--silent", StringComparison.OrdinalIgnoreCase) ||
                          a.Equals("--update", StringComparison.OrdinalIgnoreCase)))
        {
            Log("Silent update execution requested.");
            try
            {
                RunSilentUpdate(args.Any(a => a.Equals("/RESTART", StringComparison.OrdinalIgnoreCase) || a.Equals("--restart", StringComparison.OrdinalIgnoreCase)));
                Log("Silent update completed successfully.");
            }
            catch (Exception ex)
            {
                Log($"Silent update error: {ex}");
            }
            return;
        }

        try
        {
            Log("Creating App instance...");
            var app = new App();
            app.InitializeComponent();

            app.DispatcherUnhandledException += (s, e) =>
            {
                Log($"DispatcherUnhandledException: {e.Exception}");
                e.Handled = true;
            };

            Log("Running App...");
            int exitCode = app.Run();
            Log($"App.Run exited with code: {exitCode}");
        }
        catch (Exception ex)
        {
            Log($"FATAL in Main: {ex}");
            MessageBox.Show($"Startup Error:\n\n{ex.Message}\n\nSee log: {LogFile}", "Charmly Setup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void RunSilentUpdate(bool restart)
    {
        string targetDir = GetInstallDirectory();
        Log($"Silent update target directory: {targetDir}");

        InstallerEngine.Install(
            targetDir,
            enableStartup: true,
            createDesktopShortcut: true,
            createStartMenuShortcut: true,
            onProgress: (pct, msg) => Log($"[{pct}%] {msg}"));

        if (restart)
        {
            string exePath = Path.Combine(targetDir, "Charmly.exe");
            if (!File.Exists(exePath))
            {
                exePath = Path.Combine(targetDir, "Hangly.exe");
            }

            if (File.Exists(exePath))
            {
                Log($"Restarting application from {exePath}...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                });
            }
        }
    }

    private static string GetInstallDirectory()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Charmly");
            if (key?.GetValue("InstallLocation") is string loc && Directory.Exists(loc))
            {
                return loc;
            }
        }
        catch { }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Charmly");
    }
}
