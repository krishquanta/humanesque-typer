namespace Humanesque.Models;

public class AppSettings
{
    public string ActiveProfileId { get; set; } = "average";
    public List<TypingProfile> CustomProfiles { get; set; } = new();
    public bool IsFirstRun { get; set; } = true;
    public bool ShowFloatingPill { get; set; } = true;
    public bool PlayCompletionSound { get; set; } = true;
    public bool UseForegroundFallbackWhenBlocked { get; set; } = false;
    public int ForegroundCountdownSeconds { get; set; } = 3;

    // Hotkey bindings (Virtual Key Code strings)
    public string HotkeyPauseResume { get; set; } = "F8";
    public string HotkeyEmergencyAbort { get; set; } = "F9";
    public string HotkeyLockTarget { get; set; } = "Ctrl+Shift+T";
}
