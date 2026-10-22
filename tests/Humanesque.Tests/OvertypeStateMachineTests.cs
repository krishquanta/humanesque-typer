using Humanesque.Core;
using Xunit;

namespace Humanesque.Tests;

public class OvertypeStateMachineTests
{
    [Fact]
    public void OvertypeStateMachine_CalculatesCorrectBackspaceCount()
    {
        var sm = new OvertypeCorrectionStateMachine();

        // Mistyped 'h' (intended 'g')
        sm.StartTypo('g');

        // User overtypes 2 more characters before realizing: 'e', 'l'
        sm.RecordOvertype('e');
        sm.RecordOvertype('l');

        // Backspaces required = 1 (typo) + 2 (overtypes) = 3
        Assert.Equal(3, sm.GetBackspaceCount());

        // Replay sequence must be 'g', 'e', 'l'
        var replay = sm.GetReplaySequence();
        Assert.Equal(new[] { 'g', 'e', 'l' }, replay);
    }

    [Fact]
    public void OvertypeStateMachine_Reset_ClearsState()
    {
        var sm = new OvertypeCorrectionStateMachine();
        sm.StartTypo('a');
        sm.RecordOvertype('b');

        sm.Reset();

        Assert.False(sm.IsInTypoRecovery);
        Assert.Equal('\0', sm.IntendedFirstChar);
        Assert.Equal(1, sm.GetBackspaceCount()); // 1 + 0 = 1
    }
}
