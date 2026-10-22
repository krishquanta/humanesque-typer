using Humanesque.Models;

namespace Humanesque.Core;

public class BiomimeticCadence
{
    private readonly Random _rng = new();

    /// <summary>
    /// Calculates the natural human inter-keystroke interval in milliseconds.
    /// Incorporates Gaussian variation, punctuation delays, thinking gaps, and muscle-memory acceleration.
    /// </summary>
    public int CalculateKeyInterval(char currentChar, string fullText, int currentIndex, TypingProfile profile)
    {
        // 1. Calculate baseline milliseconds per character from Target WPM (standard formula: 1 word = 5 characters)
        double meanDelay = 60000.0 / (profile.TargetWpm * 5.0);
        double stdDev = profile.WpmVariance > 0 ? (meanDelay * (profile.WpmVariance / profile.TargetWpm)) : (meanDelay * 0.25);

        // 2. Generate Gaussian noise via Box-Muller transform
        double u1 = Math.Clamp(_rng.NextDouble(), 0.0001, 0.9999);
        double u2 = _rng.NextDouble();
        double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

        double baseInterval = meanDelay + (z0 * stdDev);

        // 3. Muscle memory burst acceleration on common digraphs/words
        if (NGramBurstDetector.IsInBurstSequence(fullText, currentIndex))
        {
            baseInterval *= profile.CommonWordBurstFactor;
        }

        // Clamp normal keystrokes to natural physical bounds [25ms, 450ms]
        int interval = (int)Math.Clamp(baseInterval, 25.0, 450.0);

        // 4. Contextual punctuation pauses
        if (currentChar == '.' || currentChar == '!' || currentChar == '?')
        {
            interval += _rng.Next(profile.PeriodPauseMinMs, profile.PeriodPauseMaxMs + 1);
        }
        else if (currentChar == ',' || currentChar == ';' || currentChar == ':')
        {
            interval += _rng.Next(profile.CommaPauseMinMs, profile.CommaPauseMaxMs + 1);
        }
        else if (currentChar == '\n' || currentChar == '\r')
        {
            interval += _rng.Next(profile.ParagraphPauseMinMs, profile.ParagraphPauseMaxMs + 1);
        }

        return interval;
    }

    public int GetReactionHesitation(TypingProfile profile)
    {
        return _rng.Next(profile.ReactionDelayMinMs, profile.ReactionDelayMaxMs + 1);
    }

    public int GetBackspaceInterval(TypingProfile profile)
    {
        return _rng.Next(profile.BackspaceDelayMinMs, profile.BackspaceDelayMaxMs + 1);
    }
}
