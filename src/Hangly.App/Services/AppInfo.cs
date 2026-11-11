//
//  AppInfo.cs
//  Hangly
//
//  What this build is, read from the build rather than written twice.
//

using System.Reflection;

namespace Hangly.App.Services;

/// <summary>Identity and configuration, read out of the assembly.</summary>
/// <remarks>
/// Everything here is written once, in the project file, and read back at runtime. The
/// About page showing a version that disagrees with the binary is a small thing that
/// makes everything else on the page look untrustworthy.
/// </remarks>
public static class AppInfo
{
    private static readonly Assembly Self = typeof(AppInfo).Assembly;

    public static string Name => "Charmly";

    /// <summary>Marketing version, e.g. "2.0.0".</summary>
    public static string Version { get; } =
        Self.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            is { } informational && informational.Length > 0
            ? informational.Split('+')[0]
            : Self.GetName().Version?.ToString(3) ?? "0.0.0";

    public static string BuildNumber { get; } = Metadata("HanglyBuildNumber") is { Length: > 0 } build
        ? build
        : "1";

    public static string Copyright { get; } =
        Self.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
        ?? "Copyright © 2026 Tarun";

    /// <summary>The Google Analytics Web stream's Measurement ID (<c>G-…</c>), empty when this build has none.</summary>
    public static string Ga4MeasurementId { get; } = Metadata("HanglyGa4MeasurementId") ?? string.Empty;

    /// <summary>The stream's Measurement Protocol API secret, empty when this build has none.</summary>
    public static string Ga4ApiSecret { get; } = Metadata("HanglyGa4ApiSecret") ?? string.Empty;

    /// <summary>Major.minor.build.patch, such as <c>10.0.19045.6456</c>.</summary>
    /// <remarks>
    /// <see cref="Environment.OSVersion"/> always reports the last part as 0, so the registry
    /// could not tell a PC three years behind on updates from a current one. The patch level
    /// is the UBR value Windows keeps in the registry; without it, the old answer stands.
    /// </remarks>
    public static string WindowsVersion { get; } = ReadWindowsVersion();

    /// <summary>The installation registry's URL, empty when this build has none.</summary>
    public static string RegistryUrl { get; } = Metadata("HanglyRegistryUrl") ?? string.Empty;

    /// <summary>The machine's architecture in the registry's vocabulary: "arm64" or "x64", as on macOS.</summary>
    public static string Architecture { get; } =
        System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64
            ? "arm64"
            : "x64";

    public static string WebsiteUrl => "https://itstarun.me";

    /// <summary>The creator's site, as every creator credit spells it.</summary>
    public static string CreatorSiteUrl => "https://itstarun.me";

    /// <summary>The creator's handle, written the one way it is written.</summary>
    public static string CreatorHandle => "Tarun";

    /// <summary>The four things the creator makes, in the order macOS lists them.</summary>
    public static string CreatorDisciplines => "Photography • Film • Design • Code";

    public static string GitHubUrl => "https://github.com/tarun33333/Charmly";

    public static string GitHubRepo => "tarun33333/Charmly";

    public static string GitHubApiReleasesUrl => $"https://api.github.com/repos/{GitHubRepo}/releases";

    /// <summary>Where the update feed lives.</summary>
    public static string UpdateFeedUrl => LocalUpdateFeed ?? GitHubApiReleasesUrl;

    /// <summary>A folder of packages this build updates from, if it was built with one.</summary>
    public static string? LocalUpdateFeed { get; } =
        Metadata("HanglyUpdateFeed") is { Length: > 0 } folder ? folder : null;

    public static string ReleaseNotesUrl =>
        "https://github.com/tarun33333/Charmly/releases";

    /// <summary>The creator's UPI address.</summary>
    public static string UpiId => "";

    public static string SupportUrl => "";

    /// <summary>Where "Suggest a charm" writes to.</summary>
    public static string SuggestMailUrl =>
        "mailto:tarun@example.com?subject=Charmly%20charm%20suggestion";

    private static string? Metadata(string key) => Self
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(attribute => attribute.Key == key)
        ?.Value;

    private static string ReadWindowsVersion()
    {
        Version version = Environment.OSVersion.Version;
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("UBR") is int patch and > 0)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}.{patch}";
            }
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // Unreadable is not worth failing over; the build number alone is what was sent before.
        }

        return version.ToString();
    }
}
