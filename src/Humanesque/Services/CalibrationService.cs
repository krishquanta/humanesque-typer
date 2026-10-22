using Humanesque.Models;

namespace Humanesque.Services;

public class CalibrationResult
{
    public double MeasuredWpm { get; set; }
    public double AccuracyPercent { get; set; }
    public TypingProfile CalibratedProfile { get; set; } = null!;
}

public class CalibrationService
{
    public static readonly string SampleText =
        "The quick brown fox jumps over the lazy dog while typing in the background with natural cadence.";

    public CalibrationResult EvaluateTypingTest(string expectedText, string userTypedText, TimeSpan duration)
    {
        int correctChars = 0;
        int minLength = Math.Min(expectedText.Length, userTypedText.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (expectedText[i] == userTypedText[i])
            {
                correctChars++;
            }
        }

        double minutes = Math.Max(duration.TotalMinutes, 0.05);
        double wordsTyped = correctChars / 5.0;
        double wpm = Math.Round(wordsTyped / minutes, 1);
        double accuracy = userTypedText.Length > 0
            ? Math.Round((double)correctChars / userTypedText.Length * 100.0, 1)
            : 100.0;

        double typoRate = Math.Clamp(100.0 - accuracy, 0.5, 4.5);

        var calibrated = new TypingProfile
        {
            Id = "calibrated",
            Name = "My Calibrated Profile",
            Description = $"Auto-tuned to your natural typing pace ({wpm:F0} WPM with {accuracy:F0}% accuracy).",
            TargetWpm = Math.Max(wpm, 20.0),
            WpmVariance = Math.Max(wpm * 0.12, 4.0),
            TypoRatePercent = typoRate,
            MinOvertypeCount = 1,
            MaxOvertypeCount = wpm > 70 ? 2 : 3,
            ReactionDelayMinMs = (int)Math.Clamp(300.0 - (wpm * 1.5), 120.0, 400.0),
            ReactionDelayMaxMs = (int)Math.Clamp(550.0 - (wpm * 2.0), 220.0, 650.0),
            BackspaceDelayMinMs = (int)Math.Clamp(100.0 - (wpm * 0.5), 40.0, 120.0),
            BackspaceDelayMaxMs = (int)Math.Clamp(180.0 - (wpm * 0.8), 70.0, 220.0),
            CommonWordBurstFactor = Math.Clamp(0.75 - (wpm * 0.002), 0.5, 0.85),
            CommaPauseMinMs = 150,
            CommaPauseMaxMs = 320,
            PeriodPauseMinMs = 350,
            PeriodPauseMaxMs = 750,
            ParagraphPauseMinMs = 800,
            ParagraphPauseMaxMs = 1800
        };

        return new CalibrationResult
        {
            MeasuredWpm = wpm,
            AccuracyPercent = accuracy,
            CalibratedProfile = calibrated
        };
    }
}
