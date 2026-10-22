namespace Humanesque.Models;

public record TargetWindowInfo
{
    public required IntPtr Handle { get; init; }
    public required IntPtr EditControlHandle { get; init; }
    public required string WindowTitle { get; init; }
    public required string ProcessName { get; init; }
    public required string ClassName { get; init; }
    public required int ProcessId { get; init; }
    public bool IsModernApp { get; init; }

    public string DisplayText => string.IsNullOrWhiteSpace(WindowTitle)
        ? $"[{ProcessName}] (Handle: 0x{Handle.ToInt64():X})"
        : $"{WindowTitle} ({ProcessName})";
}
