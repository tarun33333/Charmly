//
//  Updater.cs
//  Charmly
//
//  Native in-app update check and background updater using GitHub Releases.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Hangly.Core.Analytics;

namespace Hangly.App.Services;

/// <summary>What an update check found.</summary>
/// <param name="Version">The version available, or null when there is none.</param>
/// <param name="Message">What to tell the user.</param>
/// <param name="Notes">Markdown release notes.</param>
public readonly record struct UpdateCheck(string? Version, string Message, string? Notes = null)
{
    public bool HasUpdate => Version is not null;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
}

/// <summary>Finds updates from GitHub Releases, downloads them with progress, and updates Charmly.</summary>
public sealed class Updater
{
    public static string Channel =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? "win-arm64"
            : "win-x64";

    private readonly string feedUrl;
    private string? pendingVersion;
    private string? pendingNotes;
    private string? pendingAssetUrl;
    private string? pendingAssetName;

    private string? downloadedVersion;
    private string? downloadedNotes;
    private string? downloadedFilePath;

    private Task<bool>? downloading;
    private UpdateTrigger trigger = UpdateTrigger.Quiet;
    private string? reportedAvailable;

    private static void Report(AnalyticsEvent analyticsEvent) => HanglyAnalytics.Log(analyticsEvent);

    /// <summary>The version downloaded and waiting for Charmly to restart, if any.</summary>
    public string? ReadyVersion => downloadedVersion;

    /// <summary>The version the last check found, downloaded or not.</summary>
    public string? AvailableVersion => pendingVersion ?? downloadedVersion;

    /// <summary>The found release's notes (Markdown) for display.</summary>
    public string? AvailableNotes => pendingNotes ?? downloadedNotes;

    /// <summary>Last reported download progress percent (0 - 100).</summary>
    public int LastDownloadPercent { get; private set; } = -1;

    /// <summary>Percent downloaded, raised while any download runs.</summary>
    public event Action<int>? DownloadProgress;

    /// <summary>Raised when a check finds something new, or a download completes or fails.</summary>
    public event Action? StateChanged;

    public Updater(string feedUrl) => this.feedUrl = feedUrl;

    /// <summary>Whether this copy was installed into the standard user programs directory.</summary>
    public static bool IsInstalled
    {
        get
        {
            try
            {
                string? exe = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe)) return false;
                string standardDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "Charmly");
                return exe.StartsWith(standardDir, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Asks GitHub Releases what exists. Never throws.</summary>
    public async Task<UpdateCheck> CheckAsync(UpdateTrigger trigger = UpdateTrigger.Quiet)
    {
        this.trigger = trigger;
        try
        {
            Diagnostics.Log($"Checking for updates via {feedUrl}...");

            List<GitHubReleaseItem> releases;

            // Check if local folder feed is configured
            if (Directory.Exists(feedUrl))
            {
                releases = CheckLocalFeed(feedUrl);
            }
            else
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd($"Charmly/{AppInfo.Version} (Windows; x64)");

                var fetched = await http.GetFromJsonAsync<List<GitHubReleaseItem>>(feedUrl).ConfigureAwait(false);
                releases = fetched ?? new List<GitHubReleaseItem>();
            }

            if (releases.Count == 0)
            {
                return new UpdateCheck(null, "Charmly is up to date.");
            }

            // Find the newest eligible release
            TryParseVersion(AppInfo.Version, out Version currentVersion);

            GitHubReleaseItem? newestRelease = null;
            Version? highestVersion = null;

            foreach (var release in releases)
            {
                if (release.Draft) continue;
                if (!TryParseVersion(release.TagName, out Version releaseVer)) continue;

                if (releaseVer > currentVersion && (highestVersion == null || releaseVer > highestVersion))
                {
                    highestVersion = releaseVer;
                    newestRelease = release;
                }
            }

            if (newestRelease == null || highestVersion == null)
            {
                return new UpdateCheck(null, "Charmly is up to date.");
            }

            string verString = newestRelease.TagName?.TrimStart('v', 'V') ?? highestVersion.ToString(3);
            pendingVersion = verString;
            pendingNotes = newestRelease.Body;

            // Select the best asset for this install mode
            SelectAsset(newestRelease);

            if (reportedAvailable != verString)
            {
                reportedAvailable = verString;
                Report(AnalyticsEvent.UpdateAvailable(verString, trigger));
            }

            StateChanged?.Invoke();
            return new UpdateCheck(verString, $"Charmly v{verString} is available.", pendingNotes);
        }
        catch (Exception ex)
        {
            Diagnostics.Log($"Update check failed: {ex.Message}");
            Report(AnalyticsEvent.UpdateFailed(UpdateStage.Check, ex.GetType().Name, trigger));
            return new UpdateCheck(null, "Couldn't check for updates just now.");
        }
    }

    private void SelectAsset(GitHubReleaseItem release)
    {
        if (release.Assets == null || release.Assets.Count == 0) return;

        bool preferInstaller = IsInstalled;

        GitHubReleaseAsset? chosen = null;

        if (preferInstaller)
        {
            // Prefer CharmlySetup.exe or any .exe asset
            chosen = release.Assets.FirstOrDefault(a => a.Name?.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase) == true)
                  ?? release.Assets.FirstOrDefault(a => a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                  ?? release.Assets.FirstOrDefault(a => a.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
        }
        else
        {
            // Prefer zip package for portable setups
            chosen = release.Assets.FirstOrDefault(a => a.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                  ?? release.Assets.FirstOrDefault(a => a.Name?.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase) == true)
                  ?? release.Assets.FirstOrDefault(a => a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);
        }

        if (chosen != null)
        {
            pendingAssetUrl = chosen.BrowserDownloadUrl;
            pendingAssetName = chosen.Name;
        }
    }

    /// <summary>Release notes formatted as plain text.</summary>
    public static string PlainNotes(string markdown) => Hangly.Core.Text.ReleaseNotes.Plain(markdown);

    /// <summary>Downloads what the last check found. Never throws.</summary>
    public Task<bool> DownloadAsync()
    {
        if (downloading is null || (downloading.IsCompleted && !downloading.Result))
        {
            downloading = DownloadOnceAsync();
        }

        return downloading;
    }

    private async Task<bool> DownloadOnceAsync()
    {
        if (string.IsNullOrEmpty(pendingAssetUrl))
        {
            return downloadedFilePath is not null;
        }

        try
        {
            string version = pendingVersion ?? "unknown";
            Report(AnalyticsEvent.UpdateDownloadStarted(version, trigger));

            string tempDir = Path.Combine(Path.GetTempPath(), "CharmlyUpdate");
            Directory.CreateDirectory(tempDir);

            string fileName = pendingAssetName ?? $"CharmlyUpdate-{version}.exe";
            string targetPath = Path.Combine(tempDir, fileName);

            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd($"Charmly/{AppInfo.Version}");
                using var response = await http.GetAsync(pendingAssetUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;

                await using var remoteStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var localFile = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

                byte[] buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await remoteStream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    await localFile.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                    totalRead += bytesRead;

                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        int pct = (int)((totalRead * 100) / totalBytes.Value);
                        if (pct != LastDownloadPercent)
                        {
                            LastDownloadPercent = pct;
                            DownloadProgress?.Invoke(pct);
                        }
                    }
                }
            }

            downloadedFilePath = targetPath;
            downloadedVersion = pendingVersion;
            downloadedNotes = pendingNotes;

            Report(AnalyticsEvent.UpdateDownloadCompleted(version, trigger));
            Diagnostics.Log($"Update {version} downloaded to {targetPath}");
            StateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Diagnostics.Log($"Update download failed: {ex.Message}");
            Report(AnalyticsEvent.UpdateFailed(UpdateStage.Download, ex.GetType().Name, trigger));
            StateChanged?.Invoke();
            return false;
        }
    }

    /// <summary>Hands a downloaded update to be applied when the app exits.</summary>
    public void ApplyOnExit()
    {
        if (downloadedFilePath == null) return;
        ApplyDownloadedUpdate(downloadedFilePath, silent: true, restart: false);
    }

    /// <summary>Installs the update quietly and restarts Charmly.</summary>
    public bool ApplyQuietlyAndRestart()
    {
        if (downloadedFilePath == null) return false;
        return ApplyDownloadedUpdate(downloadedFilePath, silent: true, restart: true);
    }

    /// <summary>Performs update in background.</summary>
    public async Task<bool> UpdateInBackgroundAsync()
    {
        if (pendingVersion == null && downloadedFilePath == null) return false;

        trigger = UpdateTrigger.Manual;
        if (downloadedFilePath == null && !await DownloadAsync().ConfigureAwait(false))
        {
            return false;
        }

        return ApplyQuietlyAndRestart();
    }

    /// <summary>Updates now with progress callback, then installs and restarts.</summary>
    public async Task<bool> UpdateNowAsync(Action<int> downloading, Action installing, CancellationToken cancel = default)
    {
        if (pendingVersion == null && downloadedFilePath == null) return false;

        trigger = UpdateTrigger.Manual;
        void OnProgress(int pct) => downloading(pct);
        DownloadProgress += OnProgress;

        try
        {
            if (downloadedFilePath == null && !await DownloadAsync().ConfigureAwait(false))
            {
                return false;
            }
        }
        finally
        {
            DownloadProgress -= OnProgress;
        }

        if (cancel.IsCancellationRequested) return false;

        installing();
        await Task.Delay(1000).ConfigureAwait(false);

        if (cancel.IsCancellationRequested) return false;

        return ApplyQuietlyAndRestart();
    }

    /// <summary>The About page's Install and restart handler.</summary>
    public async Task<string> DownloadAndApplyAsync(Action<string>? status = null)
    {
        if (pendingVersion == null && downloadedFilePath == null)
        {
            return "There's nothing to install.";
        }

        status?.Invoke("Connecting to download server…");

        void OnProgress(int pct) => status?.Invoke($"Downloading update ({pct}%)…");
        DownloadProgress += OnProgress;

        try
        {
            if (downloadedFilePath == null)
            {
                bool ok = await DownloadAsync().ConfigureAwait(false);
                if (!ok)
                {
                    return "That update couldn't be downloaded just now. Please try again later.";
                }
            }
        }
        finally
        {
            DownloadProgress -= OnProgress;
        }

        status?.Invoke("Installing update & restarting Charmly…");
        await Task.Delay(1000).ConfigureAwait(false);

        bool launched = ApplyDownloadedUpdate(downloadedFilePath!, silent: true, restart: true);
        if (launched)
        {
            // Give installer a second to spin up before current process exits
            _ = Task.Delay(1000).ContinueWith(_ => Environment.Exit(0));
            return "Restarting Charmly…";
        }

        return "Could not launch the update installer.";
    }

    private static bool ApplyDownloadedUpdate(string filePath, bool silent, bool restart)
    {
        try
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string currentDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            string currentExe = Environment.ProcessPath ?? Path.Combine(currentDir, "Charmly.exe");

            if (ext == ".exe")
            {
                string args = silent
                    ? (restart ? "/SILENT /RESTART" : "/SILENT")
                    : (restart ? "/RESTART" : "");

                Diagnostics.Log($"Launching update installer: {filePath} {args}");
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = args,
                    UseShellExecute = true
                });
                return true;
            }
            else if (ext == ".zip")
            {
                Diagnostics.Log($"Applying portable zip update: {filePath}");
                string restartCmd = restart ? $"Start-Process '{currentExe}'" : "";
                string psCommand = $"Start-Sleep -Seconds 2; Expand-Archive -Path '{filePath}' -DestinationPath '{currentDir}' -Force; Remove-Item '{filePath}' -Force; {restartCmd}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{psCommand}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Diagnostics.Log($"Failed to apply update: {ex.Message}");
            return false;
        }
    }

    private static bool TryParseVersion(string? raw, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string cleaned = raw.Trim().TrimStart('v', 'V').Split('-', '+')[0];
        string[] parts = cleaned.Split('.');
        if (parts.Length == 1) cleaned += ".0.0";
        else if (parts.Length == 2) cleaned += ".0";

        return Version.TryParse(cleaned, out version!);
    }

    private static List<GitHubReleaseItem> CheckLocalFeed(string localDir)
    {
        var list = new List<GitHubReleaseItem>();
        try
        {
            foreach (var zip in Directory.GetFiles(localDir, "Charmly-v*.zip"))
            {
                string fn = Path.GetFileNameWithoutExtension(zip);
                string ver = fn.Replace("Charmly-", "").Replace("-Windows-x64", "");
                list.Add(new GitHubReleaseItem
                {
                    TagName = ver,
                    Name = $"Charmly {ver}",
                    Body = "Local update package",
                    Assets = new List<GitHubReleaseAsset>
                    {
                        new() { Name = Path.GetFileName(zip), BrowserDownloadUrl = zip }
                    }
                });
            }
        }
        catch { }
        return list;
    }
}

internal sealed class GitHubReleaseItem
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset>? Assets { get; set; }
}

internal sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
