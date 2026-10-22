using Humanesque.Core;
using Humanesque.Models;
using Xunit;

namespace Humanesque.Tests;

public class CadenceTests
{
    private readonly BiomimeticCadence _cadence = new();

    [Theory]
    [InlineData(30.0)]
    [InlineData(60.0)]
    [InlineData(95.0)]
    public void CalculateKeyInterval_MeanDelay_ApproximatesExpectedWpmBounds(double targetWpm)
    {
        var profile = new TypingProfile
        {
            Id = "test",
            Name = "Test Profile",
            Description = "Testing",
            TargetWpm = targetWpm,
            WpmVariance = targetWpm * 0.15,
            TypoRatePercent = 0.0,
            MinOvertypeCount = 1,
            MaxOvertypeCount = 2,
            ReactionDelayMinMs = 150,
            ReactionDelayMaxMs = 300,
            BackspaceDelayMinMs = 50,
            BackspaceDelayMaxMs = 100,
            CommonWordBurstFactor = 1.0,
            CommaPauseMinMs = 0,
            CommaPauseMaxMs = 0,
            PeriodPauseMinMs = 0,
            PeriodPauseMaxMs = 0,
            ParagraphPauseMinMs = 0,
            ParagraphPauseMaxMs = 0
        };

        // Standard WPM: 1 word = 5 characters. Delay = 60,000 / (WPM * 5)
        double theoreticalMean = 60000.0 / (targetWpm * 5.0);

        const int iterations = 1000;
        double sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            sum += _cadence.CalculateKeyInterval('a', "abcdefgh", 2, profile);
        }

        double empiricalMean = sum / iterations;

        // Allow ±15% variance due to Gaussian sampling and physical bounds clamping [25ms, 450ms]
        Assert.InRange(empiricalMean, theoreticalMean * 0.85, theoreticalMean * 1.15);
    }

    [Fact]
    public void CalculateKeyInterval_Punctuation_IncreasesDelay()
    {
        var profile = TypingProfile.CreateAverage();

        int normalKeyDelay = _cadence.CalculateKeyInterval('e', "Hello world", 1, profile);
        int periodDelay = _cadence.CalculateKeyInterval('.', "Hello world.", 11, profile);

        Assert.True(periodDelay > normalKeyDelay, "Period delay should exceed normal key delay due to punctuation thinking pause.");
    }

    [Fact]
    public void CalculateKeyInterval_BurstNGrams_FasterThanAverage()
    {
        var profile = TypingProfile.CreateAverage();

        // Sample delay across non-burst vs burst 'th'
        const int samples = 500;
        double nonBurstSum = 0;
        double burstSum = 0;

        for (int i = 0; i < samples; i++)
        {
            nonBurstSum += _cadence.CalculateKeyInterval('q', "zxqjk", 2, profile);
            burstSum += _cadence.CalculateKeyInterval('h', "the fox", 1, profile);
        }

        double avgNonBurst = nonBurstSum / samples;
        double avgBurst = burstSum / samples;

        Assert.True(avgBurst < avgNonBurst, "Digraph 'th' should be accelerated by muscle memory burst detection.");
    }
}
