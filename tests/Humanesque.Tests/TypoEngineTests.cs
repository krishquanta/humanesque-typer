using Humanesque.Core;
using Humanesque.Models;
using Xunit;

namespace Humanesque.Tests;

public class TypoEngineTests
{
    private readonly SpatialTypoEngine _engine = new();

    [Theory]
    [InlineData('e', new[] { 'w', 'r', 's', 'd', '3', '4' })]
    [InlineData('a', new[] { 'q', 'w', 's', 'z' })]
    [InlineData('k', new[] { 'j', 'l', 'i', 'o', 'm', ',' })]
    public void GenerateTypoCharacter_ReturnsTrueSpatialNeighbor(char key, char[] expectedAdjacentCandidates)
    {
        for (int i = 0; i < 50; i++)
        {
            char typo = _engine.GenerateTypoCharacter(key);
            Assert.Contains(char.ToLowerInvariant(typo), expectedAdjacentCandidates);
        }
    }

    [Fact]
    public void GenerateTypoCharacter_PreservesUppercase()
    {
        for (int i = 0; i < 50; i++)
        {
            char typo = _engine.GenerateTypoCharacter('T');
            if (char.IsLetter(typo))
            {
                Assert.True(char.IsUpper(typo), $"Letter typo of uppercase 'T' should be uppercase, but was '{typo}'");
            }
        }
    }

    [Theory]
    [InlineData(' ')]
    [InlineData('\n')]
    [InlineData('\t')]
    [InlineData('.')]
    [InlineData(',')]
    [InlineData('!')]
    public void ShouldTriggerTypo_NeverTriggersOnWhitespaceOrPunctuation(char c)
    {
        var highTypoProfile = new TypingProfile
        {
            Id = "aggressive",
            Name = "Aggressive",
            Description = "Test",
            TargetWpm = 50,
            WpmVariance = 5,
            TypoRatePercent = 99.0, // 99% typo rate
            MinOvertypeCount = 1,
            MaxOvertypeCount = 2,
            ReactionDelayMinMs = 100,
            ReactionDelayMaxMs = 200,
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

        for (int i = 0; i < 100; i++)
        {
            bool triggered = _engine.ShouldTriggerTypo(c, highTypoProfile);
            Assert.False(triggered, $"Should never trigger typo on formatting or punctuation '{c}'");
        }
    }

    [Fact]
    public void GetOvertypeCount_RespectsProfileBounds()
    {
        var profile = TypingProfile.CreateAverage(); // Min: 1, Max: 2

        for (int i = 0; i < 100; i++)
        {
            int overtypes = _engine.GetOvertypeCount(profile);
            Assert.InRange(overtypes, profile.MinOvertypeCount, profile.MaxOvertypeCount);
        }
    }
}
