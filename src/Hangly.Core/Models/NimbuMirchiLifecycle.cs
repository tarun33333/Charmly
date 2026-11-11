//
//  NimbuMirchiLifecycle.cs
//  Hangly
//
//  Lifecycle of the Indian protective charm Nimbu-Mirchi (Lemon + 7 Green Chillies):
//  ages from fresh green & yellow to wilting (1 day) to dried red & brown (2 days / 48 hours).
//

using Hangly.Core.Settings;

namespace Hangly.Core.Models;

/// <summary>The three drying stages of Nimbu-Mirchi.</summary>
public enum NimbuStage
{
    /// <summary>Fresh green chillies and bright juicy yellow lemon (0 - 24 hours).</summary>
    Fresh,

    /// <summary>Chillies begin yellowing to olive, lemon turns dull ochre (24 - 48 hours).</summary>
    Wilting,

    /// <summary>Chillies fully dried into dark red (Lal Mirch) and lemon dried amber-brown (48+ hours / 2 days).</summary>
    Dried,
}

/// <summary>Manages drying calculation, artwork resolution, and refreshing for Nimbu-Mirchi.</summary>
public static class NimbuMirchiLifecycle
{
    /// <summary>Catalogue identifier for Nimbu-Mirchi.</summary>
    public const string CharmId = "nimbuMirchi";

    /// <summary>Default duration in hours (2 days / 48 hours) until the charm is fully dried.</summary>
    public const double DefaultLifespanHours = 48.0;

    /// <summary>Legacy fallback constant for 24-hour wilting threshold.</summary>
    public const double WiltingHours = 24.0;

    /// <summary>Legacy fallback constant for 48-hour dried threshold.</summary>
    public const double DriedHours = 48.0;

    /// <summary>Checks whether a charm id is Nimbu-Mirchi.</summary>
    public static bool IsNimbuMirchi(string? id) =>
        string.Equals(id, CharmId, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the configured drying lifespan in hours.</summary>
    public static double GetLifespanHours(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.NimbuMirchiLifespanHours > 0 ? settings.NimbuMirchiLifespanHours : DefaultLifespanHours;
    }

    /// <summary>Gets the duration after which chillies begin wilting (halfway through the lifespan).</summary>
    public static double GetWiltingHours(AppSettings settings) =>
        GetLifespanHours(settings) / 2.0;

    /// <summary>Gets the duration after which the charm is fully dried.</summary>
    public static double GetDriedHours(AppSettings settings) =>
        GetLifespanHours(settings);

    /// <summary>Calculates the current age in hours based on settings.</summary>
    public static double GetAgeHours(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.NimbuMirchiSimulatedHours is double simulated)
        {
            return Math.Max(0, simulated);
        }

        if (settings.NimbuMirchiHungAt is DateTime hungAt)
        {
            return Math.Max(0, (DateTime.UtcNow - hungAt).TotalHours);
        }

        return 0;
    }

    /// <summary>Determines the current drying stage of Nimbu-Mirchi.</summary>
    public static NimbuStage GetStage(AppSettings settings)
    {
        double hours = GetAgeHours(settings);
        double dried = GetDriedHours(settings);
        double wilting = GetWiltingHours(settings);

        if (hours >= dried)
        {
            return NimbuStage.Dried;
        }
        if (hours >= wilting)
        {
            return NimbuStage.Wilting;
        }
        return NimbuStage.Fresh;
    }

    /// <summary>Returns the appropriate SVG filename for the current drying stage.</summary>
    public static string GetFileName(AppSettings settings) =>
        GetFileNameForStage(GetStage(settings));

    /// <summary>Returns the SVG filename for a specific stage.</summary>
    public static string GetFileNameForStage(NimbuStage stage) => stage switch
    {
        NimbuStage.Dried => "Nimbu-mirchi-dried.svg",
        NimbuStage.Wilting => "Nimbu-mirchi-wilting.svg",
        _ => "Nimbu-mirchi.svg",
    };

    /// <summary>User-friendly status title for the current stage.</summary>
    public static string GetStageTitle(NimbuStage stage) => stage switch
    {
        NimbuStage.Dried => "Dried (Needs Refreshing)",
        NimbuStage.Wilting => "Wilting (Drying in progress)",
        _ => "Fresh & Vibrant",
    };

    /// <summary>Short descriptive label of age and condition.</summary>
    public static string GetStatusLabel(AppSettings settings)
    {
        double hours = GetAgeHours(settings);
        double lifespan = GetLifespanHours(settings);
        NimbuStage stage = GetStage(settings);

        string timeText = hours switch
        {
            < 1.0 => "Hung just now",
            < 24.0 => $"Hung {(int)hours}h ago",
            < 48.0 => $"Hung {(int)(hours / 24.0)}d {(int)(hours % 24)}h ago",
            _ => $"Hung {(int)(hours / 24.0)}d {(int)(hours % 24)}h ago",
        };

        double remainingHours = Math.Max(0, lifespan - hours);
        string remainingText = remainingHours switch
        {
            <= 0 => "Dried up",
            < 1.0 => "Dries in <1h",
            < 24.0 => $"Dries in {(int)Math.Ceiling(remainingHours)}h",
            _ => $"Dries in {(remainingHours / 24.0):F1}d",
        };

        return stage switch
        {
            NimbuStage.Dried => $"{timeText} \u2014 Dried up! Time to hang a fresh one. ({remainingText})",
            NimbuStage.Wilting => $"{timeText} \u2014 Wilting, turning olive & yellow. ({remainingText})",
            _ => $"{timeText} \u2014 Fresh green chillies & bright lemon. ({remainingText})",
        };
    }

    /// <summary>Refreshes the charm, hanging a brand new fresh green Nimbu-Mirchi.</summary>
    public static AppSettings Refresh(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings with
        {
            NimbuMirchiHungAt = DateTime.UtcNow,
            NimbuMirchiSimulatedHours = null,
        };
    }
}
