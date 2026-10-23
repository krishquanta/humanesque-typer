namespace Humanesque.Models;

public record TargetWindowInfo
{
    public required IntPtr Handle { get; init; }
    public required IntPtr EditControlHandle { get; init; }
    public string EditControlClassName { get; init; } = string.Empty;
    public required string WindowTitle { get; init; }
    public required string ProcessName { get; init; }
    public required string ClassName { get; init; }
    public required int ProcessId { get; init; }
    public bool IsModernApp { get; init; }

    public bool HasChildEditControl => EditControlHandle != IntPtr.Zero && EditControlHandle != Handle;

    public bool SupportsBackgroundTyping =>
        ProcessName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase) || HasChildEditControl;

    public string BackgroundStrategyDescription
    {
        get
        {
            if (ProcessName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase))
                return "Word COM (100% Background, Zero Focus)";
            if (HasChildEditControl)
                return $"Win32 Direct Message ({EditControlClassName} - 100% Background)";
            return "Window Message (Background)";
        }
    }

    public string DisplayText => string.IsNullOrWhiteSpace(WindowTitle)
        ? $"[{ProcessName}] (Handle: 0x{Handle.ToInt64():X})"
        : $"{WindowTitle} ({ProcessName})";
}
