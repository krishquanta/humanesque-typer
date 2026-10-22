using Humanesque.Services;
using Xunit;

namespace Humanesque.Tests;

public class CalibrationServiceTests
{
    private readonly CalibrationService _service = new();

    [Fact]
    public void EvaluateTypingTest_CalculatesWpmAndAccuracyAccurately()
    {
        string sample = "The quick brown fox jumps over the lazy dog.";
        string typed = "The quick brown fox jumps over the lazy dog."; // 100% accurate, 44 chars = 8.8 words

        // 15 seconds = 0.25 minutes. 8.8 / 0.25 = ~35.2 WPM
        var result = _service.EvaluateTypingTest(sample, typed, TimeSpan.FromSeconds(15));

        Assert.Equal(100.0, result.AccuracyPercent);
        Assert.InRange(result.MeasuredWpm, 34.0, 36.5);
        Assert.NotNull(result.CalibratedProfile);
        Assert.Equal("My Calibrated Profile", result.CalibratedProfile.Name);
    }

    [Fact]
    public void EvaluateTypingTest_DetectsTypos_AdjustsTypoRate()
    {
        string sample = "Hello World";
        string typed = "Hxllo World"; // 1 mistake in 11 chars -> ~90.9% accuracy

        var result = _service.EvaluateTypingTest(sample, typed, TimeSpan.FromSeconds(5));

        Assert.True(result.AccuracyPercent < 100.0);
        Assert.True(result.CalibratedProfile.TypoRatePercent > 1.0);
    }
}
