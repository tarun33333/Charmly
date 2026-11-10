using System;
using System.IO;
using System.Windows;

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
}
