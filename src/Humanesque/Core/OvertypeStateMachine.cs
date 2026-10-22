namespace Humanesque.Core;

public class OvertypeCorrectionStateMachine
{
    private readonly List<char> _overtypedChars = new();
    public bool IsInTypoRecovery { get; private set; }
    public char IntendedFirstChar { get; private set; }

    public void StartTypo(char intendedChar)
    {
        _overtypedChars.Clear();
        IntendedFirstChar = intendedChar;
        IsInTypoRecovery = true;
    }

    public void RecordOvertype(char c)
    {
        _overtypedChars.Add(c);
    }

    public int GetBackspaceCount()
    {
        // Must backspace the erroneous first key + all subsequent overtyped keys
        return 1 + _overtypedChars.Count;
    }

    public List<char> GetReplaySequence()
    {
        var replay = new List<char> { IntendedFirstChar };
        replay.AddRange(_overtypedChars);
        return replay;
    }

    public void Reset()
    {
        _overtypedChars.Clear();
        IsInTypoRecovery = false;
        IntendedFirstChar = '\0';
    }
}
