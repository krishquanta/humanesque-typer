namespace Humanesque.Models;

public record TypingProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required double TargetWpm { get; init; }
    public required double WpmVariance { get; init; } // Standard deviation

    // Typo Simulation Parameters
    public required double TypoRatePercent { get; init; } // 0.0 to 10.0%
    public required int MinOvertypeCount { get; init; } // Extra characters typed before noticing
    public required int MaxOvertypeCount { get; init; }
    public required int ReactionDelayMinMs { get; init; } // Hesitation pause before backspacing
    public required int ReactionDelayMaxMs { get; init; }
    public required int BackspaceDelayMinMs { get; init; } // Inter-backspace speed
    public required int BackspaceDelayMaxMs { get; init; }

    // Cadence & Pause Modifiers
    public required double CommonWordBurstFactor { get; init; } // e.g. 0.6 = 40% faster
    public required int CommaPauseMinMs { get; init; }
    public required int CommaPauseMaxMs { get; init; }
    public required int PeriodPauseMinMs { get; init; }
    public required int PeriodPauseMaxMs { get; init; }
    public required int ParagraphPauseMinMs { get; init; }
    public required int ParagraphPauseMaxMs { get; init; }

    public static TypingProfile CreateSlow() => new()
    {
        Id = "slow",
        Name = "Slow Typer",
        Description = "Cautious, contemplative typing with frequent pauses and occasional typos.",
        TargetWpm = 32.0,
        WpmVariance = 6.0,
        TypoRatePercent = 2.8,
        MinOvertypeCount = 1,
        MaxOvertypeCount = 3,
        ReactionDelayMinMs = 280,
        ReactionDelayMaxMs = 550,
        BackspaceDelayMinMs = 90,
        BackspaceDelayMaxMs = 180,
        CommonWordBurstFactor = 0.78,
        CommaPauseMinMs = 220,
        CommaPauseMaxMs = 450,
        PeriodPauseMinMs = 500,
        PeriodPauseMaxMs = 1100,
        ParagraphPauseMinMs = 1200,
        ParagraphPauseMaxMs = 2400
    };

    public static TypingProfile CreateAverage() => new()
    {
        Id = "average",
        Name = "Average Typer",
        Description = "Natural conversational speed with balanced rhythm and realistic burst speed.",
        TargetWpm = 62.0,
        WpmVariance = 8.0,
        TypoRatePercent = 1.6,
        MinOvertypeCount = 1,
        MaxOvertypeCount = 2,
        ReactionDelayMinMs = 200,
        ReactionDelayMaxMs = 420,
        BackspaceDelayMinMs = 60,
        BackspaceDelayMaxMs = 130,
        CommonWordBurstFactor = 0.65,
        CommaPauseMinMs = 150,
        CommaPauseMaxMs = 320,
        PeriodPauseMinMs = 350,
        PeriodPauseMaxMs = 800,
        ParagraphPauseMinMs = 800,
        ParagraphPauseMaxMs = 1800
    };

    public static TypingProfile CreateFast() => new()
    {
        Id = "fast",
        Name = "Fast Typer",
        Description = "Experienced touch-typer with rapid bursts on common words and snappy corrections.",
        TargetWpm = 96.0,
        WpmVariance = 11.0,
        TypoRatePercent = 0.9,
        MinOvertypeCount = 1,
        MaxOvertypeCount = 2,
        ReactionDelayMinMs = 140,
        ReactionDelayMaxMs = 280,
        BackspaceDelayMinMs = 45,
        BackspaceDelayMaxMs = 90,
        CommonWordBurstFactor = 0.52,
        CommaPauseMinMs = 100,
        CommaPauseMaxMs = 220,
        PeriodPauseMinMs = 250,
        PeriodPauseMaxMs = 550,
        ParagraphPauseMinMs = 500,
        ParagraphPauseMaxMs = 1200
    };
}
