using System;
using Hangly.Core.Models;
using Hangly.Core.Settings;
using Xunit;

namespace Hangly.Core.Tests;

public class NimbuMirchiLifecycleTests
{
    [Fact]
    public void RecognizesNimbuMirchiId()
    {
        Assert.True(NimbuMirchiLifecycle.IsNimbuMirchi("nimbuMirchi"));
        Assert.True(NimbuMirchiLifecycle.IsNimbuMirchi("NimbuMirchi"));
        Assert.False(NimbuMirchiLifecycle.IsNimbuMirchi("hamsa"));
        Assert.False(NimbuMirchiLifecycle.IsNimbuMirchi(null));
    }

    [Fact]
    public void NewlyHungStartsFresh()
    {
        var settings = new AppSettings { NimbuMirchiHungAt = DateTime.UtcNow };

        Assert.Equal(NimbuStage.Fresh, NimbuMirchiLifecycle.GetStage(settings));
        Assert.Equal("Nimbu-mirchi.svg", NimbuMirchiLifecycle.GetFileName(settings));
    }

    [Fact]
    public void TransitionsToWiltingAfterOneDay()
    {
        // 25 hours old (Day 2)
        var settings = new AppSettings { NimbuMirchiHungAt = DateTime.UtcNow.AddHours(-25) };

        Assert.Equal(NimbuStage.Wilting, NimbuMirchiLifecycle.GetStage(settings));
        Assert.Equal("Nimbu-mirchi-wilting.svg", NimbuMirchiLifecycle.GetFileName(settings));
    }

    [Fact]
    public void TransitionsToDriedAfterTwoDays()
    {
        // 49 hours old (Day 2+)
        var settings = new AppSettings { NimbuMirchiHungAt = DateTime.UtcNow.AddHours(-49) };

        Assert.Equal(NimbuStage.Dried, NimbuMirchiLifecycle.GetStage(settings));
        Assert.Equal("Nimbu-mirchi-dried.svg", NimbuMirchiLifecycle.GetFileName(settings));
    }

    [Fact]
    public void SimulatedHoursOverridesRealTimeForTesting()
    {
        var settings = new AppSettings
        {
            NimbuMirchiHungAt = DateTime.UtcNow,
            NimbuMirchiSimulatedHours = 52.0, // simulate 2+ days
        };

        Assert.Equal(NimbuStage.Dried, NimbuMirchiLifecycle.GetStage(settings));
        Assert.Equal("Nimbu-mirchi-dried.svg", NimbuMirchiLifecycle.GetFileName(settings));

        var freshSimulation = settings with { NimbuMirchiSimulatedHours = 2.0 };
        Assert.Equal(NimbuStage.Fresh, NimbuMirchiLifecycle.GetStage(freshSimulation));

        var wiltingSimulation = settings with { NimbuMirchiSimulatedHours = 26.0 };
        Assert.Equal(NimbuStage.Wilting, NimbuMirchiLifecycle.GetStage(wiltingSimulation));
    }

    [Fact]
    public void RefreshResetsToCurrentTimeAndClearsSimulation()
    {
        var driedSettings = new AppSettings
        {
            NimbuMirchiHungAt = DateTime.UtcNow.AddHours(-100),
            NimbuMirchiSimulatedHours = 55.0,
        };

        var refreshed = NimbuMirchiLifecycle.Refresh(driedSettings);

        Assert.Null(refreshed.NimbuMirchiSimulatedHours);
        Assert.NotNull(refreshed.NimbuMirchiHungAt);
        Assert.Equal(NimbuStage.Fresh, NimbuMirchiLifecycle.GetStage(refreshed));
        Assert.Equal("Nimbu-mirchi.svg", NimbuMirchiLifecycle.GetFileName(refreshed));
    }

    [Fact]
    public void HangingNimbuMirchiInitializesHungTimestamp()
    {
        var baseSettings = new AppSettings();
        Assert.Null(baseSettings.NimbuMirchiHungAt);

        var hungSettings = Hanging.Hang(baseSettings, 0, "nimbuMirchi");
        Assert.NotNull(hungSettings.NimbuMirchiHungAt);
        Assert.Equal(NimbuStage.Fresh, NimbuMirchiLifecycle.GetStage(hungSettings));
    }
}
