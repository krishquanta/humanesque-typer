using Humanesque.Models;

namespace Humanesque.Core;

public class SpatialTypoEngine
{
    private readonly Random _rng = new();

    /// <summary>
    /// Determines whether the current keystroke should trigger an erroneous finger slip.
    /// Punctuation, newlines, and whitespace are excluded from typo generation to avoid disruptive formatting errors.
    /// </summary>
    public bool ShouldTriggerTypo(char c, TypingProfile profile)
    {
        if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsControl(c))
        {
            return false;
        }

        double roll = _rng.NextDouble() * 100.0;
        return roll < profile.TypoRatePercent;
    }

    /// <summary>
    /// Returns an adjacent QWERTY key simulating a realistic human finger slip.
    /// </summary>
    public char GenerateTypoCharacter(char intendedChar)
    {
        return KeyboardLayoutMatrix.GetSpatialNeighbor(intendedChar, _rng);
    }

    /// <summary>
    /// Calculates how many subsequent characters the user will type before noticing the mistake ("overtype lag").
    /// </summary>
    public int GetOvertypeCount(TypingProfile profile)
    {
        return _rng.Next(profile.MinOvertypeCount, profile.MaxOvertypeCount + 1);
    }
}
